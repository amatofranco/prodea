using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
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
            .Where(p => p.TournamentId == tournamentId && p.UserId == userId)
            .SumAsync(p => p.PointsEarned);

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var allPoints = await db.Predictions
            .Where(p => p.TournamentId == tournamentId && participants.Contains(p.UserId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        int rank = allPoints.FindIndex(x => x.UserId == userId) + 1;

        var matchdayBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == tournamentId && mb.UserId == userId)
            .OrderByDescending(mb => mb.Matchday)
            .ToListAsync();

        var accumulativeBadges = await db.AccumulativeBadges
            .Where(ab => ab.TournamentId == tournamentId && ab.UserId == userId)
            .ToListAsync();

        return Ok(new PlayerProfileDto(
            userId, user.Username, user.AvatarUrl, totalPoints, rank,
            matchdayBadges.Select(mb => new MatchdayBadgeDto(
                mb.Matchday,
                mb.BadgeType.ToString(),
                BadgeService.GetEmoji(mb.BadgeType),
                mb.BadgeType.ToString(),
                mb.PointsInMatchday,
                BadgeService.GetRandomPhrase(mb.BadgeType),
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
