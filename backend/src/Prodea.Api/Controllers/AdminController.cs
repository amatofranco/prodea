using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(
    ProdeaDbContext db,
    IWebHostEnvironment env,
    FixtureService fixtureService,
    PollingStatusService pollingStatus) : ControllerBase
{
    [HttpGet("matches")]
    public async Task<IActionResult> ListMatches(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromQuery] string? phase = null)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var query = db.Matches.AsQueryable();
        if (phase != null && Enum.TryParse<MatchPhase>(phase, true, out var p))
            query = query.Where(m => m.Phase == p);

        var matches = await query
            .OrderBy(m => m.MatchDate)
            .Select(m => new { m.Id, m.ExternalId, m.HomeTeam, m.AwayTeam, m.Phase, m.Matchday, m.Status, m.MatchDate, m.HomeScore, m.AwayScore })
            .ToListAsync();

        return Ok(matches);
    }

    [HttpPost("seed-fixture")]
    public async Task<IActionResult> SeedFixture(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromQuery] bool force = false)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var (count, source) = await fixtureService.ImportAsync(force);
        if (count == 0 && source == "ya cargado")
            return Conflict(new { message = "Fixture ya cargado. Usá ?force=true para reimportar." });

        return Ok(new { message = $"{count} partidos cargados", source });
    }

    [HttpGet("backups")]
    public async Task<IActionResult> ListBackups(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var backups = await db.PredictionBackups
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.CreatedAt, b.Count })
            .ToListAsync();

        return Ok(backups);
    }

    [HttpPost("backups/{id}/restore")]
    public async Task<IActionResult> RestoreBackup(
        int id,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var backup = await db.PredictionBackups.FindAsync(id);
        if (backup == null) return NotFound(new { message = "Backup no encontrado" });

        var payload = JsonSerializer.Deserialize<BackupPayload>(backup.JsonData, JsonOptions);
        if (payload?.Predictions == null)
            return UnprocessableEntity(new { message = "JSON del backup no válido" });

        var existingKeys = await db.Predictions
            .Select(p => new { p.UserId, p.MatchId })
            .ToListAsync();
        var existing = existingKeys.ToHashSet();

        var toRestore = payload.Predictions
            .Where(p => !existing.Contains(new { p.UserId, p.MatchId }))
            .Select(p => new Prediction
            {
                UserId             = p.UserId,
                MatchId            = p.MatchId,
                PredictedHomeScore = p.PredictedHomeScore,
                PredictedAwayScore = p.PredictedAwayScore,
                PointsEarned       = p.PointsEarned,
                CreatedAt          = p.CreatedAt,
                UpdatedAt          = p.UpdatedAt,
            })
            .ToList();

        if (toRestore.Count > 0)
        {
            db.Predictions.AddRange(toRestore);
            await db.SaveChangesAsync();
        }

        return Ok(new
        {
            message  = $"{toRestore.Count} predicciones restauradas",
            restored = toRestore.Count,
            skipped  = payload.Predictions.Count - toRestore.Count,
            backupDate = backup.CreatedAt,
        });
    }

    [HttpPost("matches/{id}/simulate")]
    public async Task<IActionResult> SimulateMatch(
        int id,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromBody] SimulateMatchRequest request)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var match = await db.Matches.FindAsync(id);
        if (match == null) return NotFound(new { message = "Partido no encontrado" });

        match.Status     = request.Status;
        match.HomeScore  = request.HomeScore;
        match.AwayScore  = request.AwayScore;
        match.Minute     = request.Minute;
        if (request.MatchDate.HasValue)
            match.MatchDate = request.MatchDate.Value;
        if (request.HomeTeam != null) match.HomeTeam = request.HomeTeam;
        if (request.AwayTeam != null) match.AwayTeam = request.AwayTeam;

        await db.SaveChangesAsync();

        return Ok(new
        {
            message   = $"Partido {id} actualizado",
            matchId   = match.Id,
            homeTeam  = match.HomeTeam,
            awayTeam  = match.AwayTeam,
            status    = match.Status.ToString(),
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            matchDate = match.MatchDate,
        });
    }

    [HttpPost("matches/{id}/finalize")]
    public async Task<IActionResult> FinalizeMatch(
        int id,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromBody] FinalizeMatchRequest request)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var match = await db.Matches.FindAsync(id);
        if (match == null) return NotFound(new { message = "Partido no encontrado" });

        match.HomeScore = request.HomeScore;
        match.AwayScore = request.AwayScore;
        match.Status = MatchStatus.Finished;

        // winner: "home" | "away" — para penales cuando scores son iguales
        if (request.Winner == "home") match.Winner = match.HomeTeam;
        else if (request.Winner == "away") match.Winner = match.AwayTeam;
        else match.Winner = null;

        await db.SaveChangesAsync();

        string? winnerSide = null;
        if (match.HomeScore == match.AwayScore && match.Winner != null)
            winnerSide = match.Winner == match.HomeTeam ? "home" : "away";

        var predictions = await db.Predictions
            .Where(p => p.MatchId == match.Id)
            .ToListAsync();

        foreach (var pred in predictions)
        {
            pred.PointsEarned = ScoringService.CalculatePoints(pred, match.HomeScore!.Value, match.AwayScore!.Value, winnerSide);
            pred.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        var badgeService = new BadgeService(db);
        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId).Distinct().ToListAsync();
        foreach (var tid in tournamentIds)
            await badgeService.AssignMatchdayBadgesAsync(tid, match.Phase, match.Matchday ?? 0);

        if (match.Phase == MatchPhase.Final)
        {
            string? champion = match.HomeScore > match.AwayScore ? match.HomeTeam
                : match.AwayScore > match.HomeScore ? match.AwayTeam
                : match.Winner;
            if (champion != null)
            {
                var picks = await db.ChampionPicks
                    .Where(cp => cp.CountryName == champion && cp.PointsEarned == 0).ToListAsync();
                foreach (var pick in picks) pick.PointsEarned = 10;
                await db.SaveChangesAsync();
            }
        }

        return Ok(new
        {
            message      = $"Partido {id} finalizado con scoring",
            matchId      = match.Id,
            homeTeam     = match.HomeTeam,
            awayTeam     = match.AwayTeam,
            homeScore    = match.HomeScore,
            awayScore    = match.AwayScore,
            winner       = match.Winner,
            winnerSide,
            predictionsScored = predictions.Count,
            details = predictions.Select(p => new { p.UserId, p.PredictedHomeScore, p.PredictedAwayScore, p.PredictedPenaltyWinner, p.PointsEarned }),
        });
    }

    [HttpPost("simulate-jornada")]
    public async Task<IActionResult> SimulateJornada(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromBody] SimulateJornadaRequest request)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var phase = Enum.Parse<MatchPhase>(request.Phase);
        var matches = await db.Matches
            .Where(m => m.Phase == phase && (request.Matchday == 0 ? m.Matchday == null : m.Matchday == request.Matchday))
            .ToListAsync();

        if (matches.Count == 0)
            return NotFound(new { message = $"No hay partidos para {request.Phase} matchday {request.Matchday}" });

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == request.TournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        if (participants.Count == 0)
            return BadRequest(new { message = "El torneo no tiene participantes" });

        var rng = new Random(request.Seed ?? Environment.TickCount);

        // Si force, limpiar predicciones y badges existentes de esta jornada
        if (request.Force)
        {
            var matchIds2 = matches.Select(m => m.Id).ToList();
            var toDelete = await db.Predictions
                .Where(p => matchIds2.Contains(p.MatchId) && participants.Contains(p.UserId))
                .ToListAsync();
            db.Predictions.RemoveRange(toDelete);

            var badgesToDelete = await db.MatchdayBadges
                .Where(mb => mb.TournamentId == request.TournamentId && mb.Phase == phase.ToString() && mb.Matchday == request.Matchday)
                .ToListAsync();
            db.MatchdayBadges.RemoveRange(badgesToDelete);

            await db.SaveChangesAsync();
        }

        // Asignar resultados a los partidos (sin tocar MatchDate — siempre usamos las fechas reales de la API)
        foreach (var match in matches)
        {
            match.HomeScore = rng.Next(0, 5);
            match.AwayScore = rng.Next(0, 5);
            match.Status = MatchStatus.Finished;
        }
        await db.SaveChangesAsync();

        // Crear predicciones variadas para cada participante
        var matchIds = matches.Select(m => m.Id).ToList();
        var existingPreds = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && matchIds.Contains(p.MatchId))
            .ToListAsync();
        var existingKeys = existingPreds.Select(p => (p.UserId, p.MatchId)).ToHashSet();

        int predCount = 0;
        for (int i = 0; i < participants.Count; i++)
        {
            int userId = participants[i];
            // Primer usuario: mayoría de aciertos (Crack candidate)
            // Último usuario: todo 0-0 (Payaso candidate)
            // Resto: mixto
            foreach (var match in matches)
            {
                if (existingKeys.Contains((userId, match.Id))) continue;

                int ph, pa;
                if (i == 0)
                {
                    // Casi exacto — replica el resultado real con pequeña variación
                    ph = match.HomeScore!.Value + rng.Next(-1, 2);
                    pa = match.AwayScore!.Value + rng.Next(-1, 2);
                    ph = Math.Max(0, ph); pa = Math.Max(0, pa);
                }
                else if (i == participants.Count - 1)
                {
                    // Pésimo — siempre falla
                    ph = (match.HomeScore!.Value + 2 + rng.Next(1, 3)) % 6;
                    pa = (match.AwayScore!.Value + 2 + rng.Next(1, 3)) % 6;
                }
                else
                {
                    ph = rng.Next(0, 4);
                    pa = rng.Next(0, 4);
                }

                var pred = new Prediction
                {
                    UserId = userId,
                    MatchId = match.Id,
                    PredictedHomeScore = ph,
                    PredictedAwayScore = pa,
                };
                pred.PointsEarned = ScoringService.CalculatePoints(pred, match.HomeScore!.Value, match.AwayScore!.Value);
                db.Predictions.Add(pred);
                predCount++;
            }
        }
        await db.SaveChangesAsync();

        // Asignar badges
        var badgeService = new BadgeService(db);
        await badgeService.AssignMatchdayBadgesAsync(request.TournamentId, phase, request.Matchday);

        return Ok(new
        {
            message = $"Jornada simulada: {matches.Count} partidos, {participants.Count} participantes, {predCount} predicciones nuevas",
            matches = matches.Count,
            participants = participants.Count,
            predictionsCreated = predCount,
        });
    }

    [HttpPost("recalculate-all/{tournamentId}")]
    public async Task<IActionResult> RecalculateAll(
        int tournamentId,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (env.IsProduction())
            return NotFound();

        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (expectedKey == null || adminKey != expectedKey)
            return Forbid();

        // Recalcular puntos de predicciones para todos los partidos finalizados
        var finishedMatches = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished && m.HomeScore != null && m.AwayScore != null)
            .ToListAsync();

        int predsUpdated = 0;
        foreach (var match in finishedMatches)
        {
            var predictions = await db.Predictions
                .Where(p => p.MatchId == match.Id)
                .ToListAsync();

            string? winnerSide = null;
            if (match.HomeScore == match.AwayScore && match.Winner != null)
                winnerSide = match.Winner == match.HomeTeam ? "home" : "away";

            foreach (var pred in predictions)
            {
                pred.PointsEarned = ScoringService.CalculatePoints(pred, match.HomeScore!.Value, match.AwayScore!.Value, winnerSide);
                predsUpdated++;
            }
        }
        await db.SaveChangesAsync();

        // Recalcular badges
        var badgeService = new BadgeService(db);
        var phaseMatchdays = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Select(m => new { m.Phase, Matchday = m.Matchday ?? 0 })
            .Distinct()
            .ToListAsync();

        foreach (var pm in phaseMatchdays)
            await badgeService.AssignMatchdayBadgesAsync(tournamentId, pm.Phase, pm.Matchday);

        return Ok(new { message = $"Recalculado: {predsUpdated} predicciones, {phaseMatchdays.Count} jornadas con badges." });
    }

    [HttpPost("recalculate-badges/{tournamentId}")]
    public async Task<IActionResult> RecalculateBadges(
        int tournamentId,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var badgeService = new BadgeService(db);

        // Recalcular badges de jornada para todas las fases/jornadas ya terminadas
        var finishedPhaseMatchdays = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Select(m => new { m.Phase, Matchday = m.Matchday ?? 0 })
            .Distinct()
            .ToListAsync();

        int matchdayCount = 0;
        foreach (var pm in finishedPhaseMatchdays)
        {
            await badgeService.AssignMatchdayBadgesAsync(tournamentId, pm.Phase, pm.Matchday);
            matchdayCount++;
        }

        return Ok(new { message = $"Badges recalculados para torneo {tournamentId}: {matchdayCount} jornadas/fases procesadas." });
    }

    [HttpGet("polling-status")]
    public async Task<IActionResult> GetPollingStatus()
    {
        var inProgress = await db.Matches
            .Where(m => m.Status == MatchStatus.InProgress)
            .CountAsync();

        var upcoming = await db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddHours(1))
            .CountAsync();

        var staleThreshold = TimeSpan.FromMinutes(15);
        var isStale = inProgress > 0
            && (pollingStatus.LastSuccessfulPoll == null
                || DateTime.UtcNow - pollingStatus.LastSuccessfulPoll > staleThreshold);

        return Ok(new
        {
            inProgressMatches = inProgress,
            upcomingInNextHour = upcoming,
            apiAvailable = pollingStatus.ApiAvailable,
            lastSuccessfulPoll = pollingStatus.LastSuccessfulPoll,
            isStale,
        });
    }

    [HttpPost("finalize-group-stage")]
    public async Task<IActionResult> FinalizeGroupStage(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (env.IsProduction())
            return NotFound();

        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (expectedKey == null || adminKey != expectedKey)
            return Forbid();

        var matches = await db.Matches
            .Where(m => m.Phase == MatchPhase.Group)
            .ToListAsync();

        var rng = new Random();
        foreach (var m in matches)
        {
            m.HomeScore = rng.Next(0, 5);
            m.AwayScore = rng.Next(0, 5);
            m.Status = MatchStatus.Finished;
            m.Minute = null;
        }
        await db.SaveChangesAsync();

        return Ok(new { message = $"{matches.Count} partidos de fase de grupos finalizados." });
    }

    [HttpPost("cleanup-production")]
    public async Task<IActionResult> CleanupProduction(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromQuery] string? confirm = null)
    {
        if (env.IsProduction())
            return NotFound();

        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (expectedKey == null || adminKey != expectedKey)
            return Forbid();

        if (confirm != "si")
            return BadRequest(new { message = "Agregá ?confirm=si para confirmar la limpieza." });

        // Borrar todo excepto el usuario admin
        await db.MatchdayBadges.ExecuteDeleteAsync();
        await db.AccumulativeBadges.ExecuteDeleteAsync();
        await db.PredictionBackups.ExecuteDeleteAsync();
        await db.Predictions.ExecuteDeleteAsync();
        await db.TournamentParticipants.ExecuteDeleteAsync();
        await db.Tournaments.ExecuteDeleteAsync();
        await db.ChampionPicks.ExecuteDeleteAsync();

        // Borrar usuarios que no sean francoamato92@gmail.com
        var deletedUsers = await db.Users
            .Where(u => u.Email != "francoamato92@gmail.com")
            .ExecuteDeleteAsync();

        // Re-importar fixture limpio desde la API
        var (matchCount, source) = await fixtureService.ImportAsync(force: true);

        return Ok(new
        {
            message = "Producción limpia.",
            usersDeleted = deletedUsers,
            matchesReimported = matchCount,
            fixtureSource = source,
        });
    }

    [HttpPost("reset-simulation")]
    public async Task<IActionResult> ResetSimulation(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        // Re-importar fixture desde la API: restaura fechas reales, equipos y estados
        var (matchCount, source) = await fixtureService.ImportAsync(force: true);

        // Reset prediction points
        await db.Predictions
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PointsEarned, 0));

        // Delete all badges
        await db.MatchdayBadges.ExecuteDeleteAsync();
        await db.AccumulativeBadges.ExecuteDeleteAsync();

        // Reset champion pick points
        await db.ChampionPicks
            .ExecuteUpdateAsync(s => s.SetProperty(cp => cp.PointsEarned, 0));

        return Ok(new { message = $"Simulación reseteada: fixture re-importado desde {source} ({matchCount} partidos), predicciones a 0 pts, badges eliminados." });
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public record SimulateMatchRequest(MatchStatus Status, int? HomeScore, int? AwayScore, int? Minute = null, DateTime? MatchDate = null, string? HomeTeam = null, string? AwayTeam = null);
    public record FinalizeMatchRequest(int HomeScore, int AwayScore, string? Winner = null); // Winner: "home" | "away" para penales
    public record SimulateJornadaRequest(int TournamentId, string Phase, int Matchday, int? Seed = null, bool Force = false);

    private record BackupPrediction(
        int UserId, int MatchId,
        int PredictedHomeScore, int PredictedAwayScore,
        int PointsEarned, DateTime CreatedAt, DateTime UpdatedAt);

    private record BackupPayload(
        DateTime GeneratedAt, int Count,
        List<BackupPrediction> Predictions);
}
