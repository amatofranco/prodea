using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Hubs;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId}/matches")]
[Authorize]
public class MatchesController(ProdeaDbContext db, IHubContext<TournamentHub> hub) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<MatchWithPredictionDto>>> GetMatches(int tournamentId)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!isMember) return Forbid();

        var matches = await db.Matches.OrderBy(m => m.MatchDate).ToListAsync();
        var predictions = await db.Predictions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var predMap = predictions.ToDictionary(p => p.MatchId);

        return Ok(matches.Select(m =>
        {
            predMap.TryGetValue(m.Id, out var pred);
            var goals = m.GoalsJson != null
                ? JsonSerializer.Deserialize<List<GoalDto>>(m.GoalsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                : null;
            return new MatchWithPredictionDto(
                m.Id, m.HomeTeam, m.AwayTeam, m.HomeTeamLabel, m.AwayTeamLabel,
                m.HomeTeamFlag, m.AwayTeamFlag,
                m.MatchDate, m.Phase.ToString(), m.Matchday, m.HomeScore, m.AwayScore,
                m.Status.ToString(),
                pred == null ? null : new PredictionDto(pred.Id, pred.PredictedHomeScore, pred.PredictedAwayScore, pred.PointsEarned, pred.PredictedPenaltyWinner),
                m.Minute,
                goals,
                m.LivePhase
            );
        }));
    }

    [HttpGet("{matchId}/predictions")]
    public async Task<ActionResult<List<MatchPredictionDto>>> GetMatchPredictions(int tournamentId, int matchId)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!isMember) return Forbid();

        var match = await db.Matches.FindAsync(matchId);
        if (match == null) return NotFound();
        if (match.Status != MatchStatus.Finished)
            return BadRequest(new { message = "El partido no terminó aún." });

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Include(tp => tp.User)
            .ToListAsync();

        var participantIds = participants.Select(tp => tp.UserId).ToList();

        var predictions = await db.Predictions
            .Where(p => p.MatchId == matchId && participantIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);

        return Ok(participants
            .Select(tp =>
            {
                predictions.TryGetValue(tp.UserId, out var pred);
                var fullName = (tp.User.FirstName != null || tp.User.LastName != null)
                    ? $"{tp.User.FirstName} {tp.User.LastName}".Trim() : null;
                return new MatchPredictionDto(
                    tp.UserId, tp.User.Username, fullName,
                    pred?.PredictedHomeScore, pred?.PredictedAwayScore,
                    pred?.PointsEarned ?? 0,
                    pred?.PredictedPenaltyWinner
                );
            })
            .OrderByDescending(p => p.PointsEarned)
            .ThenBy(p => p.Username)
            .ToList());
    }

    [HttpPost("{matchId}/result")]
    public async Task<IActionResult> UpdateMatchResult(int tournamentId, int matchId, UpdateMatchResultRequest request)
    {
        var userId = CurrentUserId;
        var tournament = await db.Tournaments.FindAsync(tournamentId);
        if (tournament == null) return NotFound();
        if (tournament.AdminUserId != userId) return Forbid();

        var match = await db.Matches.FindAsync(matchId);
        if (match == null) return NotFound(new { message = "Partido no encontrado" });

        match.HomeScore = request.HomeScore;
        match.AwayScore = request.AwayScore;
        match.Status = MatchStatus.Finished;
        match.FinishedAt = DateTime.UtcNow;
        match.LastUpdatedAt = DateTime.UtcNow;

        // For penalty matches: admin passes winner = "home" | "away"
        if (request.HomeScore == request.AwayScore && request.Winner != null)
            match.Winner = request.Winner == "home" ? match.HomeTeam : match.AwayTeam;

        await db.SaveChangesAsync();

        string? winnerSide = (request.HomeScore == request.AwayScore && request.Winner != null)
            ? request.Winner : null;

        var predictions = await db.Predictions
            .Where(p => p.MatchId == matchId)
            .ToListAsync();

        foreach (var pred in predictions)
        {
            pred.PointsEarned = ScoringService.CalculatePoints(pred, request.HomeScore, request.AwayScore, winnerSide);
            pred.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        var push = HttpContext.RequestServices.GetRequiredService<PushNotificationService>();
        var badgeService = new BadgeService(db);
        var allTournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId)
            .Distinct()
            .ToListAsync();
        foreach (var tid in allTournamentIds)
            await badgeService.AssignMatchdayBadgesAsync(tid, match.Phase, match.Matchday ?? 0, push);

        if (match.Phase == MatchPhase.Final)
        {
            string? champion = null;
            if (request.HomeScore > request.AwayScore) champion = match.HomeTeam;
            else if (request.AwayScore > request.HomeScore) champion = match.AwayTeam;
            else champion = match.Winner; // penalty case

            if (champion != null)
            {
                var winningPicks = await db.ChampionPicks
                    .Where(cp => cp.CountryName == champion && cp.PointsEarned == 0)
                    .ToListAsync();
                foreach (var pick in winningPicks) pick.PointsEarned = 10;
                await db.SaveChangesAsync();
            }
        }

        var payload = new
        {
            matchId = match.Id,
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            status = match.Status.ToString(),
        };

        var affectedTournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId)
            .Distinct()
            .ToListAsync();

        foreach (var tid in affectedTournamentIds)
            await hub.Clients.Group($"tournament-{tid}").SendAsync("MatchUpdated", payload);

        return Ok(new { message = "Resultado cargado y puntos calculados" });
    }
}
