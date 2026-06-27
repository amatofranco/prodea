using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Filters;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminKey]
public class AdminTestController(
    ProdeaDbContext db,
    IWebHostEnvironment env,
    FixtureService fixtureService) : ControllerBase
{
    [HttpPost("simulate-matchday")]
    public async Task<IActionResult> SimulateMatchday([FromBody] SimulateMatchdayRequest request)
    {
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

        foreach (var match in matches)
        {
            match.HomeScore = rng.Next(0, 5);
            match.AwayScore = rng.Next(0, 5);
            match.Status = MatchStatus.Finished;
            match.FinishedAt = DateTime.UtcNow;
            match.LastUpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();

        var matchIds = matches.Select(m => m.Id).ToList();
        var existingPreds = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && matchIds.Contains(p.MatchId))
            .ToListAsync();
        var existingKeys = existingPreds.Select(p => (p.UserId, p.MatchId)).ToHashSet();

        int predCount = 0;
        for (int i = 0; i < participants.Count; i++)
        {
            int userId = participants[i];
            foreach (var match in matches)
            {
                if (existingKeys.Contains((userId, match.Id))) continue;

                int ph, pa;
                if (i == 0)
                {
                    ph = Math.Max(0, match.HomeScore!.Value + rng.Next(-1, 2));
                    pa = Math.Max(0, match.AwayScore!.Value + rng.Next(-1, 2));
                }
                else if (i == participants.Count - 1)
                {
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
    public async Task<IActionResult> RecalculateAll(int tournamentId)
    {
        if (env.IsProduction())
            return NotFound();

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
    public async Task<IActionResult> RecalculateBadges(int tournamentId)
    {
        var badgeService = new BadgeService(db);

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

    [HttpPost("finalize-group-stage")]
    public async Task<IActionResult> FinalizeGroupStage()
    {
        if (env.IsProduction())
            return NotFound();

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
            m.FinishedAt = DateTime.UtcNow;
            m.LastUpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();

        int predsUpdated = 0;
        foreach (var m in matches)
        {
            var predictions = await db.Predictions.Where(p => p.MatchId == m.Id).ToListAsync();
            foreach (var pred in predictions)
            {
                pred.PointsEarned = ScoringService.CalculatePoints(pred, m.HomeScore!.Value, m.AwayScore!.Value);
                predsUpdated++;
            }
        }
        await db.SaveChangesAsync();

        return Ok(new { message = $"{matches.Count} partidos finalizados, {predsUpdated} predicciones recalculadas." });
    }

    [HttpPost("cleanup-production")]
    public async Task<IActionResult> CleanupProduction([FromQuery] string? confirm = null)
    {
        if (env.IsProduction())
            return NotFound();

        if (confirm != "si")
            return BadRequest(new { message = "Agregá ?confirm=si para confirmar la limpieza." });

        await db.MatchdayBadges.ExecuteDeleteAsync();
        await db.AccumulativeBadges.ExecuteDeleteAsync();
        await db.PredictionBackups.ExecuteDeleteAsync();
        await db.Predictions.ExecuteDeleteAsync();
        await db.TournamentParticipants.ExecuteDeleteAsync();
        await db.Tournaments.ExecuteDeleteAsync();
        await db.ChampionPicks.ExecuteDeleteAsync();

        var deletedUsers = await db.Users
            .Where(u => u.Email != "francoamato92@gmail.com")
            .ExecuteDeleteAsync();

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
    public async Task<IActionResult> ResetSimulation()
    {
        var (matchCount, source) = await fixtureService.ImportAsync(force: true);

        await db.Predictions
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PointsEarned, 0));

        await db.MatchdayBadges.ExecuteDeleteAsync();
        await db.AccumulativeBadges.ExecuteDeleteAsync();

        await db.ChampionPicks
            .ExecuteUpdateAsync(s => s.SetProperty(cp => cp.PointsEarned, 0));

        return Ok(new { message = $"Simulación reseteada: fixture re-importado desde {source} ({matchCount} partidos), predicciones a 0 pts, badges eliminados." });
    }

    public record SimulateMatchdayRequest(int TournamentId, string Phase, int Matchday, int? Seed = null, bool Force = false);
}
