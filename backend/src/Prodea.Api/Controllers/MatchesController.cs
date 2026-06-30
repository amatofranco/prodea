using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Extensions;
using Prodea.Api.Hubs;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId}/matches")]
[Authorize]
public class MatchesController(ProdeaDbContext db, IHubContext<TournamentHub> hub, MatchFinalizationService finalizationService) : AuthorizedControllerBase
{

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

        var myChampionPick = await db.ChampionPicks.FirstOrDefaultAsync(cp => cp.UserId == userId);

        return Ok(matches.Select(m =>
        {
            predMap.TryGetValue(m.Id, out var pred);
            var goals = m.GoalsJson != null
                ? JsonSerializer.Deserialize<List<GoalDto>>(m.GoalsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                : null;
            bool isFinal = m.Phase == MatchPhase.Final;
            return new MatchWithPredictionDto(
                m.Id, m.HomeTeam, m.AwayTeam, m.HomeTeamLabel, m.AwayTeamLabel,
                m.HomeTeamFlag, m.AwayTeamFlag,
                m.MatchDate, m.Phase.ToString(), m.Matchday, m.HomeScore, m.AwayScore,
                m.Status.ToString(),
                pred == null ? null : new PredictionDto(pred.Id, pred.PredictedHomeScore, pred.PredictedAwayScore, pred.PointsEarned, pred.PredictedPenaltyWinner),
                m.Minute,
                goals,
                m.LivePhase,
                isFinal ? myChampionPick?.CountryName : null,
                isFinal && myChampionPick != null ? myChampionPick.PointsEarned : null,
                m.HomePenaltyScore,
                m.AwayPenaltyScore
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
            return BadRequest(new { message = "match_not_finished" });

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Include(tp => tp.User)
            .ToListAsync();

        var participantIds = participants.Select(tp => tp.UserId).ToList();

        var predictions = await db.Predictions
            .Where(p => p.MatchId == matchId && participantIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);

        var championPicks = match.Phase == MatchPhase.Final
            ? await db.ChampionPicks.Where(cp => participantIds.Contains(cp.UserId)).ToDictionaryAsync(cp => cp.UserId)
            : new Dictionary<int, ChampionPick>();

        return Ok(participants
            .Select(tp =>
            {
                predictions.TryGetValue(tp.UserId, out var pred);
                championPicks.TryGetValue(tp.UserId, out var champPick);
                var fullName = tp.User.FullName();
                return new MatchPredictionDto(
                    tp.UserId, tp.User.Username, fullName,
                    pred?.PredictedHomeScore, pred?.PredictedAwayScore,
                    pred?.PointsEarned ?? 0,
                    pred?.PredictedPenaltyWinner,
                    champPick?.CountryName,
                    champPick != null ? champPick.PointsEarned : null
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

        if (request.HomeScore == request.AwayScore && request.Winner != null)
            match.Winner = request.Winner == "home" ? match.HomeTeam : match.AwayTeam;

        await db.SaveChangesAsync();

        var result = await finalizationService.ProcessAsync(match);

        var payload = new
        {
            matchId = match.Id,
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            status = match.Status.ToString(),
        };
        foreach (var tid in result.TournamentIds)
            await hub.Clients.Group($"tournament-{tid}").SendAsync("MatchUpdated", payload);

        return Ok(new { message = "Resultado cargado y puntos calculados" });
    }
}
