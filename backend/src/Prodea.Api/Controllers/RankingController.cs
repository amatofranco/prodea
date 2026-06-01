using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.DTOs;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RankingController(ProdeaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetGlobalRanking()
    {
        var predictionPoints = await db.Predictions
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .ToListAsync();

        var championPoints = await db.ChampionPicks
            .GroupBy(cp => cp.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Max(cp => cp.PointsEarned) })
            .ToListAsync();

        var predMap = predictionPoints.ToDictionary(x => x.UserId, x => x.Total);
        var champMap = championPoints.ToDictionary(x => x.UserId, x => x.Total);

        var userIds = predMap.Keys.Union(champMap.Keys).ToHashSet();

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var ranked = users
            .Select(u => new
            {
                User = u,
                Total = predMap.GetValueOrDefault(u.Id, 0) + champMap.GetValueOrDefault(u.Id, 0)
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.User.Username)
            .ToList();

        return Ok(ranked.Select((x, i) =>
        {
            var fullName = (x.User.FirstName != null || x.User.LastName != null)
                ? $"{x.User.FirstName} {x.User.LastName}".Trim()
                : null;
            return new LeaderboardEntryDto(i + 1, x.User.Id, x.User.Username, fullName, x.User.AvatarUrl, x.Total, null, null);
        }).ToList());
    }
}
