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
        await SyncKnockoutIfGroupCompleteAsync(match);
        var predictions = await ScorePredictionsAsync(match);
        await ScoreChampionPicksAsync(match);
        var (tournamentIds, newlyBadged) = await AssignBadgesAcrossTournamentsAsync(match);

        if (sendCardNotifications && newlyBadged.Count > 0)
            await badgeService.SendCardNotificationsPublicAsync(match.Phase, match.Matchday ?? 0, newlyBadged, pushService);

        return new FinalizationResult(predictions, tournamentIds);
    }

    private async Task SyncKnockoutIfGroupCompleteAsync(Match match)
    {
        if (match.Phase != MatchPhase.Group || match.Group == null) return;
        var groupDone = !await db.Matches
            .AnyAsync(m => m.Phase == MatchPhase.Group && m.Group == match.Group && m.Status != MatchStatus.Finished);
        if (groupDone)
            await fixtureService.UpdateKnockoutTeamNamesAsync();
    }

    private async Task<List<Prediction>> ScorePredictionsAsync(Match match)
    {
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
        return predictions;
    }

    private async Task ScoreChampionPicksAsync(Match match)
    {
        if (match.Phase != MatchPhase.Final || !match.HomeScore.HasValue) return;
        var champion = MatchResultHelper.DetermineChampion(match);
        if (champion == null) return;

        var picks = await db.ChampionPicks
            .Where(cp => cp.CountryName == champion && cp.PointsEarned == 0)
            .ToListAsync();
        foreach (var pick in picks) pick.PointsEarned = 10;
        await db.SaveChangesAsync();
    }

    private async Task<(List<int> TournamentIds, Dictionary<int, int> NewlyBadged)> AssignBadgesAcrossTournamentsAsync(Match match)
    {
        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId).Distinct().ToListAsync();

        var newlyBadged = new Dictionary<int, int>();
        foreach (var tid in tournamentIds)
        {
            var newUserIds = await badgeService.AssignMatchdayBadgesAsync(tid, match.Phase, match.Matchday ?? 0);
            foreach (var uid in newUserIds)
                newlyBadged.TryAdd(uid, tid);
            if (match.Phase == MatchPhase.Final)
                await badgeService.AwardTournamentResultBadgesAsync(tid);
        }

        return (tournamentIds, newlyBadged);
    }

    public record FinalizationResult(List<Prediction> ScoredPredictions, List<int> TournamentIds);
}
