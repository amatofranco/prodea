using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
            .Where(p => p.TournamentId == tournamentId && p.UserId == userId)
            .ToListAsync();

        var predMap = predictions.ToDictionary(p => p.MatchId);

        return Ok(matches.Select(m =>
        {
            predMap.TryGetValue(m.Id, out var pred);
            return new MatchWithPredictionDto(
                m.Id, m.HomeTeam, m.AwayTeam, m.HomeTeamFlag, m.AwayTeamFlag,
                m.MatchDate, m.Phase.ToString(), m.Matchday, m.HomeScore, m.AwayScore,
                m.Status.ToString(),
                pred == null ? null : new PredictionDto(pred.Id, pred.PredictedHomeScore, pred.PredictedAwayScore, pred.PointsEarned)
            );
        }));
    }

    [HttpPost("{matchId}/predictions")]
    public async Task<ActionResult<PredictionDto>> SubmitPrediction(int tournamentId, int matchId, SubmitPredictionRequest request)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!isMember) return Forbid();

        var match = await db.Matches.FindAsync(matchId);
        if (match == null) return NotFound(new { message = "Partido no encontrado" });
        if (match.Status != MatchStatus.Scheduled)
            return BadRequest(new { message = "Las predicciones están cerradas para este partido" });

        var existing = await db.Predictions.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.TournamentId == tournamentId && p.MatchId == matchId);

        if (existing != null)
        {
            existing.PredictedHomeScore = request.PredictedHomeScore;
            existing.PredictedAwayScore = request.PredictedAwayScore;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new PredictionDto(existing.Id, existing.PredictedHomeScore, existing.PredictedAwayScore, existing.PointsEarned));
        }

        var prediction = new Prediction
        {
            UserId = userId,
            TournamentId = tournamentId,
            MatchId = matchId,
            PredictedHomeScore = request.PredictedHomeScore,
            PredictedAwayScore = request.PredictedAwayScore,
        };

        db.Predictions.Add(prediction);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMatches), new { tournamentId },
            new PredictionDto(prediction.Id, prediction.PredictedHomeScore, prediction.PredictedAwayScore, 0));
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

        await db.SaveChangesAsync();

        var predictions = await db.Predictions
            .Where(p => p.MatchId == matchId && p.TournamentId == tournamentId)
            .ToListAsync();

        foreach (var pred in predictions)
        {
            pred.PointsEarned = ScoringService.CalculatePoints(pred, request.HomeScore, request.AwayScore);
            pred.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        if (match.Matchday.HasValue)
        {
            var badgeService = new BadgeService(db);
            await badgeService.AssignMatchdayBadgesAsync(tournamentId, match.Matchday.Value);
        }

        var payload = new
        {
            matchId = match.Id,
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            status = match.Status.ToString(),
        };

        await hub.Clients.Group($"tournament-{tournamentId}").SendAsync("MatchUpdated", payload);

        return Ok(new { message = "Resultado cargado y puntos calculados" });
    }
}
