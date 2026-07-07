using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Filters;
using Prodea.Api.Hubs;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminKey]
public class AdminController(
    ProdeaDbContext db,
    FixtureService fixtureService,
    PollingStatusService pollingStatus,
    EspnApiClient espn,
    IHubContext<TournamentHub> hub,
    MatchFinalizationService finalizationService,
    BackupService backupService) : ControllerBase
{
    [HttpPost("backups/run")]
    public async Task<IActionResult> RunBackup(CancellationToken ct)
    {
        var result = await backupService.RunNowAsync(ct);
        return Ok(new { message = result });
    }

    [HttpGet("matches")]
    public async Task<IActionResult> ListMatches([FromQuery] string? phase = null)
    {
        var query = db.Matches.AsQueryable();
        if (phase != null && Enum.TryParse<MatchPhase>(phase, true, out var p))
            query = query.Where(m => m.Phase == p);

        var matches = await query
            .OrderBy(m => m.MatchDate)
            .Select(m => new { m.Id, m.ExternalId, m.HomeTeam, m.AwayTeam, m.HomeTeamLabel, m.AwayTeamLabel, m.Group, m.Phase, m.Matchday, m.Status, m.MatchDate, m.HomeScore, m.AwayScore, m.StartedAt, m.FinishedAt, m.LastUpdatedAt, m.ReminderSent, m.ResultNotificationSent, m.Minute, m.LivePhase, m.GoalsJson })
            .ToListAsync();

        return Ok(matches);
    }

    [HttpPost("seed-fixture")]
    public async Task<IActionResult> SeedFixture([FromQuery] bool force = false)
    {
        var (count, source) = await fixtureService.ImportAsync(force);
        if (count == 0 && source == "already loaded")
            return Conflict(new { message = "Fixture already loaded. Use ?force=true to reimport." });

        return Ok(new { message = $"{count} matches loaded", source });
    }

    [HttpPost("sync-knockout-teams")]
    public async Task<IActionResult> SyncKnockoutTeams()
    {
        var updated = await fixtureService.UpdateKnockoutTeamNamesAsync();
        return Ok(new { message = $"{updated} knockout match(es) updated from football-data.org" });
    }

    [HttpGet("backups")]
    public async Task<IActionResult> ListBackups()
    {
        var backups = await db.PredictionBackups
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.CreatedAt, b.Count })
            .ToListAsync();

        return Ok(backups);
    }

    [HttpPost("backups/{id}/restore")]
    public async Task<IActionResult> RestoreBackup(int id)
    {
        var backup = await db.PredictionBackups.FindAsync(id);
        if (backup == null) return NotFound(new { message = "Backup not found" });

        var payload = JsonSerializer.Deserialize<BackupPayload>(backup.JsonData, JsonOptions);
        if (payload?.Predictions == null)
            return UnprocessableEntity(new { message = "Invalid backup JSON" });

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
            message  = $"{toRestore.Count} predictions restored",
            restored = toRestore.Count,
            skipped  = payload.Predictions.Count - toRestore.Count,
            backupDate = backup.CreatedAt,
        });
    }

    [HttpPost("matches/{id}/simulate")]
    public async Task<IActionResult> SimulateMatch(int id, [FromBody] SimulateMatchRequest request)
    {
        var match = await db.Matches.FindAsync(id);
        if (match == null) return NotFound(new { message = "Match not found" });

        var wasFinished = match.Status == MatchStatus.Finished;

        match.Status     = request.Status;
        match.HomeScore  = request.HomeScore;
        match.AwayScore  = request.AwayScore;
        match.Minute     = request.Minute;
        if (request.MatchDate.HasValue)
            match.MatchDate = request.MatchDate.Value;
        if (request.HomeTeam != null) match.HomeTeam = request.HomeTeam;
        if (request.AwayTeam != null) match.AwayTeam = request.AwayTeam;
        match.LastUpdatedAt = DateTime.UtcNow;
        if (request.Status == MatchStatus.InProgress) match.StartedAt = DateTime.UtcNow;
        if (request.Status == MatchStatus.Finished) match.FinishedAt = DateTime.UtcNow;

        // Evita "puntos fantasma" en partidos que vuelven a Scheduled/InProgress durante testing
        if (wasFinished && request.Status != MatchStatus.Finished)
        {
            await db.Predictions
                .Where(p => p.MatchId == id && p.PointsEarned != 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.PointsEarned, 0));
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            message   = $"Match {id} updated",
            matchId   = match.Id,
            homeTeam  = match.HomeTeam,
            awayTeam  = match.AwayTeam,
            status    = match.Status.ToString(),
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            matchDate = match.MatchDate,
        });
    }

    [HttpDelete("matches/{id}")]
    public async Task<IActionResult> DeleteMatch(int id)
    {
        var match = await db.Matches.FindAsync(id);
        if (match == null) return NotFound(new { message = "Match not found" });

        db.Matches.Remove(match);
        await db.SaveChangesAsync();

        return Ok(new { message = $"Match {id} deleted" });
    }

    [HttpPost("matches/{id}/finalize")]
    public async Task<IActionResult> FinalizeMatch(int id, [FromBody] FinalizeMatchRequest request)
    {
        var match = await db.Matches.FindAsync(id);
        if (match == null) return NotFound(new { message = "Match not found" });

        match.HomeScore = request.HomeScore;
        match.AwayScore = request.AwayScore;
        match.Status = MatchStatus.Finished;
        match.FinishedAt = DateTime.UtcNow;
        match.LastUpdatedAt = DateTime.UtcNow;

        // "home" | "away" — para penales cuando scores son iguales
        if (request.Winner == "home") match.Winner = match.HomeTeam;
        else if (request.Winner == "away") match.Winner = match.AwayTeam;
        else match.Winner = null;

        await db.SaveChangesAsync();

        var result = await finalizationService.ProcessAsync(match, sendCardNotifications: false);

        string? winnerSide = null;
        if (match.HomeScore == match.AwayScore && match.Winner != null)
            winnerSide = match.Winner == match.HomeTeam ? "home" : "away";

        return Ok(new
        {
            message      = $"Match {id} finalized with scoring",
            matchId      = match.Id,
            homeTeam     = match.HomeTeam,
            awayTeam     = match.AwayTeam,
            homeScore    = match.HomeScore,
            awayScore    = match.AwayScore,
            winner       = match.Winner,
            winnerSide,
            predictionsScored = result.ScoredPredictions.Count,
            details = result.ScoredPredictions.Select(p => new { p.UserId, p.PredictedHomeScore, p.PredictedAwayScore, p.PredictedPenaltyWinner, p.PointsEarned }),
        });
    }

    [HttpPost("matches/{id}/fetch-goals")]
    public async Task<IActionResult> FetchGoals(int id, CancellationToken ct)
    {
        var match = await db.Matches.FindAsync(id);
        if (match == null) return NotFound(new { message = "Match not found" });

        var edtDate = match.MatchDate.AddHours(-4).Date;
        var events = await espn.FetchScoreboardAsync(edtDate, ct);
        var espnEvent = EspnApiClient.FindMatch(events, match);

        if (espnEvent == null)
            return NotFound(new { message = $"Match not found in ESPN for {edtDate:yyyy-MM-dd}" });

        var goals = await espn.FetchGoalsAsync(espnEvent.Id, ct);

        match.GoalsJson = goals.Count > 0 ? JsonSerializer.Serialize(goals) : null;
        await db.SaveChangesAsync(ct);

        if (goals.Count > 0)
        {
            var payload = new { matchId = match.Id, homeScore = match.HomeScore, awayScore = match.AwayScore, status = match.Status.ToString(), minute = match.Minute, goals = goals.Select(g => new { scorer = g.Scorer, team = g.Team, minute = g.Minute }) };
            var tids = await db.TournamentParticipants.Select(tp => tp.TournamentId).Distinct().ToListAsync(ct);
            foreach (var tid in tids)
                await hub.Clients.Group($"tournament-{tid}").SendAsync("MatchUpdated", payload, ct);
        }

        return Ok(new { message = $"{goals.Count} goals saved for {match.HomeTeam} vs {match.AwayTeam}", espnEventId = espnEvent.Id, goals });
    }

    [SkipAdminKey]
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

    [HttpPost("full-backup")]
    public async Task<IActionResult> FullBackup(CancellationToken ct)
    {
        var result = await backupService.RunFullBackupNowAsync(ct);
        return result.StartsWith("Backup__AdminEmail")
            ? BadRequest(new { message = result })
            : Ok(new { message = result });
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public record SimulateMatchRequest(MatchStatus Status, int? HomeScore, int? AwayScore, int? Minute = null, DateTime? MatchDate = null, string? HomeTeam = null, string? AwayTeam = null);
    public record FinalizeMatchRequest(int HomeScore, int AwayScore, string? Winner = null);

    private record BackupPrediction(
        int UserId, int MatchId,
        int PredictedHomeScore, int PredictedAwayScore,
        int PointsEarned, DateTime CreatedAt, DateTime UpdatedAt);

    private record BackupPayload(
        DateTime GeneratedAt, int Count,
        List<BackupPrediction> Predictions);
}
