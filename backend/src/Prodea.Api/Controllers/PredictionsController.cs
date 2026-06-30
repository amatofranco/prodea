using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/predictions")]
[Authorize]
public class PredictionsController(ProdeaDbContext db) : AuthorizedControllerBase
{
    private static readonly TimeSpan PredictionCloseBeforeKickoff = TimeSpan.FromMinutes(15);

    [HttpGet]
    public async Task<ActionResult<List<MatchWithPredictionDto>>> GetMyPredictions()
    {
        var userId = CurrentUserId;
        var matches = await db.Matches.OrderBy(m => m.MatchDate).ToListAsync();
        var predictions = await db.Predictions.Where(p => p.UserId == userId).ToListAsync();
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
                m.LivePhase,
                HomePenaltyScore: m.HomePenaltyScore,
                AwayPenaltyScore: m.AwayPenaltyScore
            );
        }));
    }

    [HttpPost("{matchId}")]
    public async Task<ActionResult<PredictionDto>> SubmitPrediction(int matchId, SubmitPredictionRequest request)
    {
        var userId = CurrentUserId;

        var match = await db.Matches.FindAsync(matchId);
        if (match == null) return NotFound(new { message = "match_not_found" });
        if (match.HomeTeam == "TBD" || match.AwayTeam == "TBD")
            return BadRequest(new { message = "teams_not_confirmed" });
        if (match.Status != MatchStatus.Scheduled)
            return BadRequest(new { message = "predictions_closed" });
        if (match.MatchDate - DateTime.UtcNow < PredictionCloseBeforeKickoff)
            return BadRequest(new { message = "predictions_close_before" });

        var existing = await db.Predictions.FirstOrDefaultAsync(p => p.UserId == userId && p.MatchId == matchId);

        // PredictedPenaltyWinner solo aplica en eliminatoria y cuando el score es empate
        var penaltyWinner = (match.Phase != MatchPhase.Group && request.PredictedHomeScore == request.PredictedAwayScore)
            ? request.PredictedPenaltyWinner
            : null;

        if (existing != null)
        {
            existing.PredictedHomeScore = request.PredictedHomeScore;
            existing.PredictedAwayScore = request.PredictedAwayScore;
            existing.PredictedPenaltyWinner = penaltyWinner;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new PredictionDto(existing.Id, existing.PredictedHomeScore, existing.PredictedAwayScore, existing.PointsEarned, existing.PredictedPenaltyWinner));
        }

        var prediction = new Prediction
        {
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = request.PredictedHomeScore,
            PredictedAwayScore = request.PredictedAwayScore,
            PredictedPenaltyWinner = penaltyWinner,
        };

        db.Predictions.Add(prediction);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyPredictions), new PredictionDto(prediction.Id, prediction.PredictedHomeScore, prediction.PredictedAwayScore, 0, penaltyWinner));
    }
}
