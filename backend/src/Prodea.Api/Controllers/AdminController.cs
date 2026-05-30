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

        // Asignar resultados a los partidos
        foreach (var match in matches)
        {
            match.HomeScore = rng.Next(0, 5);
            match.AwayScore = rng.Next(0, 5);
            match.Status = MatchStatus.Finished;
            match.MatchDate = DateTime.UtcNow.AddHours(-2);
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

    [HttpPost("recalculate-badges/{tournamentId}")]
    public async Task<IActionResult> RecalculateBadges(
        int tournamentId,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var badgeService = new BadgeService(db);
        await badgeService.RecalculateAccumulativeBadgesAsync(tournamentId);
        return Ok(new { message = $"Badges acumulativos recalculados para torneo {tournamentId}" });
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

    [HttpPost("reset-simulation")]
    public async Task<IActionResult> ResetSimulation(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        // Reset all matches to Scheduled with no score
        await db.Matches
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, MatchStatus.Scheduled)
                .SetProperty(m => m.HomeScore, (int?)null)
                .SetProperty(m => m.AwayScore, (int?)null)
                .SetProperty(m => m.Minute, (int?)null));

        // Reset all prediction points to 0
        await db.Predictions
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PointsEarned, 0));

        // Delete all matchday and accumulative badges
        await db.MatchdayBadges.ExecuteDeleteAsync();
        await db.AccumulativeBadges.ExecuteDeleteAsync();

        // Reset champion pick points to 0
        await db.ChampionPicks
            .ExecuteUpdateAsync(s => s.SetProperty(cp => cp.PointsEarned, 0));

        return Ok(new { message = "Simulación reseteada: partidos a Scheduled, predicciones a 0 pts, badges eliminados." });
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public record SimulateMatchRequest(MatchStatus Status, int? HomeScore, int? AwayScore, int? Minute = null, DateTime? MatchDate = null);
    public record SimulateJornadaRequest(int TournamentId, string Phase, int Matchday, int? Seed = null, bool Force = false);

    private record BackupPrediction(
        int UserId, int MatchId,
        int PredictedHomeScore, int PredictedAwayScore,
        int PointsEarned, DateTime CreatedAt, DateTime UpdatedAt);

    private record BackupPayload(
        DateTime GeneratedAt, int Count,
        List<BackupPrediction> Predictions);
}
