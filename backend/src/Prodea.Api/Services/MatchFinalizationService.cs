using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class MatchFinalizationService(
    ProdeaDbContext db,
    BadgeService badgeService,
    FixtureService fixtureService,
    PushNotificationService pushService)
{
    public async Task<FinalizationResult> ProcessAsync(Match match, bool sendCardNotifications = true)
    {
        if (match.Phase == MatchPhase.Group && match.Group != null)
        {
            var groupDone = !await db.Matches
                .AnyAsync(m => m.Phase == MatchPhase.Group && m.Group == match.Group && m.Status != MatchStatus.Finished);
            if (groupDone)
                await fixtureService.UpdateKnockoutTeamNamesAsync();
        }

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

        // Champion picks — must run BEFORE badges so the final leaderboard includes champion points
        if (match.Phase == MatchPhase.Final && match.HomeScore.HasValue)
        {
            var champion = MatchResultHelper.DetermineChampion(match);
            if (champion != null)
            {
                var picks = await db.ChampionPicks
                    .Where(cp => cp.CountryName == champion && cp.PointsEarned == 0)
                    .ToListAsync();
                foreach (var pick in picks) pick.PointsEarned = 10;
                await db.SaveChangesAsync();
            }
        }

        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId).Distinct().ToListAsync();

        var newlyBadgedUserTournament = new Dictionary<int, int>();
        foreach (var tid in tournamentIds)
        {
            var newUserIds = await badgeService.AssignMatchdayBadgesAsync(tid, match.Phase, match.Matchday ?? 0);
            foreach (var uid in newUserIds)
                newlyBadgedUserTournament.TryAdd(uid, tid);
            if (match.Phase == MatchPhase.Final)
                await badgeService.AwardTournamentResultBadgesAsync(tid);
        }

        if (sendCardNotifications && newlyBadgedUserTournament.Count > 0)
            await badgeService.SendCardNotificationsPublicAsync(match.Phase, match.Matchday ?? 0, newlyBadgedUserTournament, pushService);

        return new FinalizationResult(predictions, tournamentIds);
    }

    public record FinalizationResult(List<Prediction> ScoredPredictions, List<int> TournamentIds);
}
