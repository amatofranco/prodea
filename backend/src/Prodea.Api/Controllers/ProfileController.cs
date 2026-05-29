using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId}/profile")]
[Authorize]
public class ProfileController(ProdeaDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{userId}")]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(int tournamentId, int userId)
    {
        var currentUser = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == currentUser);
        if (!isMember) return Forbid();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var totalPoints = await db.Predictions
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.PointsEarned);

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var allPoints = await db.Predictions
            .Where(p => participants.Contains(p.UserId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        int rank = allPoints.FindIndex(x => x.UserId == userId) + 1;

        var matchdayBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == tournamentId && mb.UserId == userId && mb.Phase != "")
            .OrderByDescending(mb => mb.AwardedAt)
            .ToListAsync();

        // Assign occurrence index per badge type (chronological, oldest = 0) for unique phrase selection
        var occurrenceCounts = new Dictionary<MatchdayBadgeType, int>();
        var phraseIndex = matchdayBadges
            .OrderBy(mb => mb.AwardedAt)
            .ToDictionary(
                mb => (mb.Phase, mb.Matchday),
                mb =>
                {
                    occurrenceCounts.TryGetValue(mb.BadgeType, out var count);
                    occurrenceCounts[mb.BadgeType] = count + 1;
                    return count;
                });

        var accumulativeBadges = await db.AccumulativeBadges
            .Where(ab => ab.TournamentId == tournamentId && ab.UserId == userId)
            .ToListAsync();

        return Ok(new PlayerProfileDto(
            userId, user.Username, user.AvatarUrl, totalPoints, rank,
            matchdayBadges.Select(mb => new MatchdayBadgeDto(
                mb.Phase,
                mb.Matchday,
                mb.BadgeType.ToString(),
                BadgeService.GetEmoji(mb.BadgeType),
                mb.BadgeType.ToString(),
                mb.PointsInMatchday,
                BadgeService.GetPhrase(mb.BadgeType, mb.UserId, phraseIndex[(mb.Phase, mb.Matchday)]),
                mb.AwardedAt
            )).ToList(),
            accumulativeBadges.Select(ab => new AccumulativeBadgeDto(
                ab.BadgeType.ToString(),
                BadgeService.GetAccumulativeEmoji(ab.BadgeType),
                ab.BadgeType.ToString(),
                ab.AwardedAt
            )).ToList()
        ));
    }
}
