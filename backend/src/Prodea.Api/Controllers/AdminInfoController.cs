using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Filters;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminKey]
public class AdminInfoController(ProdeaDbContext db, BadgeService badgeService, PushNotificationService pushService) : ControllerBase
{
    [HttpPost("push/test")]
    public async Task<IActionResult> TestPush([FromBody] TestPushRequest? request = null)
    {
        var title = request?.Title ?? "🔔 Test notification";
        var body = request?.Body ?? "Push notification system is working.";
        var url = request?.Url ?? "/";

        var sent = await pushService.BroadcastAsync(db, title, body, url);
        return Ok(new { message = $"Sent to {sent} subscriber(s)." });
    }

    [HttpPost("push/test-card")]
    public async Task<IActionResult> TestCardPush([FromBody] TestCardPushRequest request)
    {
        var phase = Enum.Parse<MatchPhase>(request.Phase);
        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == request.TournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        if (participants.Count == 0)
            return BadRequest(new { message = "Tournament has no participants." });

        var userTournamentMap = participants.ToDictionary(uid => uid, _ => request.TournamentId);
        await badgeService.SendCardNotificationsPublicAsync(phase, request.Matchday, userTournamentMap, pushService);

        return Ok(new { message = $"Card notification sent to {participants.Count} participant(s)." });
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers()
    {
        var users = await db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsPwa,
                isGoogle = u.GoogleId != null,
                u.CreatedAt,
                predictionCount = u.Predictions.Count,
                tournamentCount = u.TournamentParticipants.Count,
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });

        await db.Predictions.Where(p => p.UserId == id).ExecuteDeleteAsync();
        await db.TournamentParticipants.Where(tp => tp.UserId == id).ExecuteDeleteAsync();
        await db.MatchdayBadges.Where(mb => mb.UserId == id).ExecuteDeleteAsync();
        await db.AccumulativeBadges.Where(ab => ab.UserId == id).ExecuteDeleteAsync();
        await db.PushSubscriptions.Where(ps => ps.UserId == id).ExecuteDeleteAsync();
        await db.ChampionPicks.Where(cp => cp.UserId == id).ExecuteDeleteAsync();
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Ok(new { message = $"User {user.Username} ({user.FirstName} {user.LastName}) deleted." });
    }

    [HttpGet("tournaments/{id}/accumulative-badges")]
    public async Task<IActionResult> ListAccumulativeBadges(int id)
    {
        var badges = await db.AccumulativeBadges
            .Where(ab => ab.TournamentId == id)
            .Select(ab => new
            {
                ab.UserId,
                username = db.Users.Where(u => u.Id == ab.UserId).Select(u => u.Username).FirstOrDefault(),
                badgeType = ab.BadgeType.ToString(),
                ab.AwardedAt,
            })
            .ToListAsync();

        return Ok(badges);
    }

    [HttpDelete("tournaments/{id}/accumulative-badges")]
    public async Task<IActionResult> DeleteAccumulativeBadges(int id)
    {
        // ExecuteDeleteAsync evita fallar con valores de BadgeType obsoletos en el enum
        var count = await db.AccumulativeBadges.Where(ab => ab.TournamentId == id).ExecuteDeleteAsync();
        return Ok(new { message = $"{count} accumulative badges deleted." });
    }

    [HttpGet("tournaments/{id}/predicted-goals")]
    public async Task<IActionResult> ListPredictedGoals(int id)
    {
        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == id)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var startingMatchDate = await db.Tournaments
            .Where(t => t.Id == id)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();

        var goals = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new
            {
                g.Key,
                username = db.Users.Where(u => u.Id == g.Key).Select(u => u.Username).FirstOrDefault(),
                goals = g.Sum(p => p.PredictedHomeScore + p.PredictedAwayScore),
                predictions = g.Count(),
            })
            .OrderByDescending(g => g.goals)
            .ToListAsync();

        return Ok(goals);
    }

    [HttpGet("tournaments/{id}/matchday-badges")]
    public async Task<IActionResult> ListMatchdayBadges(int id, [FromQuery] string? phase = null)
    {
        var query = db.MatchdayBadges.Where(mb => mb.TournamentId == id);
        if (phase != null) query = query.Where(mb => mb.Phase == phase);

        var badges = await query
            .Select(mb => new
            {
                mb.UserId,
                username = db.Users.Where(u => u.Id == mb.UserId).Select(u => u.Username).FirstOrDefault(),
                mb.Phase,
                mb.Matchday,
                badgeType = mb.BadgeType.ToString(),
                mb.PointsInMatchday,
                mb.AwardedAt,
            })
            .ToListAsync();

        return Ok(badges);
    }

    [HttpGet("tournaments")]
    public async Task<IActionResult> ListTournaments()
    {
        var tournaments = await db.Tournaments
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Code,
                adminUsername = db.Users.Where(u => u.Id == t.AdminUserId).Select(u => u.Username).FirstOrDefault(),
                participantCount = t.Participants.Count,
                participants = t.Participants.Select(tp => tp.User.Username).ToList(),
                t.CreatedAt,
            })
            .ToListAsync();

        return Ok(tournaments);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers        = await db.Users.CountAsync();
        var activeUsers       = await db.Predictions.Select(p => p.UserId).Distinct().CountAsync();
        var pwaUsers          = await db.Users.CountAsync(u => u.IsPwa);
        var totalTournaments  = await db.Tournaments.CountAsync();
        var totalPredictions  = await db.Predictions.CountAsync();
        var pushSubscriptions = await db.PushSubscriptions.CountAsync();

        return Ok(new { totalUsers, activeUsers, pwaUsers, totalTournaments, totalPredictions, pushSubscriptions });
    }

    public record TestPushRequest(string? Title, string? Body, string? Url);
    public record TestCardPushRequest(int TournamentId, string Phase, int Matchday);
}
