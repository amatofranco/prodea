using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Extensions;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId}/profile")]
[Authorize]
public class ProfileController(ProdeaDbContext db, TournamentRankingService ranking, BadgeService badgeService) : AuthorizedControllerBase
{

    [HttpGet("{userId}")]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(int tournamentId, int userId)
    {
        var currentUser = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == currentUser);
        if (!isMember) return Forbid();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var participantIds = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var startingMatchDate = await db.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();

        var data = await ranking.ComputeAsync(tournamentId, participantIds, startingMatchDate);
        var totalPoints = TournamentRankingService.TotalPoints(data, userId);
        var rank = TournamentRankingService.Rank(data, participantIds, userId);

        var (matchdayBadges, accumulativeBadges) = await badgeService.GetUserProfileBadgesAsync(tournamentId, userId);

        return Ok(new PlayerProfileDto(
            userId, user.Username, user.FullName(), user.AvatarUrl, totalPoints, rank,
            matchdayBadges, accumulativeBadges
        ));
    }

    [HttpGet("{userId}/predictions")]
    public async Task<ActionResult<List<MatchWithPredictionDto>>> GetUserPredictions(int tournamentId, int userId)
    {
        var currentUser = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == currentUser);
        if (!isMember) return Forbid();

        var tournament = await db.Tournaments.FindAsync(tournamentId);
        if (tournament == null) return NotFound();

        var matches = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished && m.MatchDate >= tournament.StartingMatchDate)
            .OrderByDescending(m => m.MatchDate)
            .ToListAsync();

        var matchIds = matches.Select(m => m.Id).ToList();
        var predictions = await db.Predictions
            .Where(p => p.UserId == userId && matchIds.Contains(p.MatchId))
            .ToListAsync();
        var predByMatch = predictions.ToDictionary(p => p.MatchId);

        return Ok(matches
            .Where(m => predByMatch.ContainsKey(m.Id))
            .Select(m =>
            {
                var pred = predByMatch[m.Id];
                return new MatchWithPredictionDto(
                    m.Id, m.HomeTeam, m.AwayTeam, m.HomeTeamLabel, m.AwayTeamLabel, m.HomeTeamFlag, m.AwayTeamFlag,
                    m.MatchDate, m.Phase.ToString(), m.Matchday, m.HomeScore, m.AwayScore, m.Status.ToString(),
                    new PredictionDto(pred.Id, pred.PredictedHomeScore, pred.PredictedAwayScore, pred.PointsEarned, pred.PredictedPenaltyWinner),
                    null
                );
            }).ToList());
    }
}
