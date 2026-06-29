using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class TournamentRankingService(ProdeaDbContext db)
{
    public record RankingData(
        Dictionary<int, int> PointsMap,
        Dictionary<int, int> ChampionPoints,
        Dictionary<int, MatchdayBadge> LastBadgeMap);

    public async Task<RankingData> ComputeAsync(int tournamentId, List<int> participantIds, DateTime startingMatchDate)
    {
        var points = await db.Predictions
            .Where(p => participantIds.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .ToListAsync();

        var championPicksList = await db.ChampionPicks
            .Where(cp => participantIds.Contains(cp.UserId))
            .ToListAsync();
        var championPoints = championPicksList
            .GroupBy(cp => cp.UserId)
            .ToDictionary(g => g.Key, g => g.Max(cp => cp.PointsEarned));

        var allBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == tournamentId && mb.Phase != "")
            .ToListAsync();

        var lastBadges = allBadges
            .GroupBy(mb => mb.UserId)
            .Select(g => g
                .OrderByDescending(mb => Enum.TryParse<MatchPhase>(mb.Phase, out var p) ? (int)p : -1)
                .ThenByDescending(mb => mb.Matchday)
                .First())
            .ToList();

        return new RankingData(
            points.ToDictionary(p => p.UserId, p => p.Total),
            championPoints,
            lastBadges.ToDictionary(b => b.UserId, b => b));
    }

    public static int TotalPoints(RankingData data, int userId) =>
        data.PointsMap.GetValueOrDefault(userId, 0) + data.ChampionPoints.GetValueOrDefault(userId, 0);

    public static int Rank(RankingData data, List<int> participantIds, int userId)
    {
        var sorted = participantIds
            .OrderByDescending(uid => TotalPoints(data, uid))
            .ToList();
        return sorted.IndexOf(userId) + 1;
    }
}
