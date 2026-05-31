using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/champion-pick")]
[Authorize]
public class ChampionPickController(ProdeaDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly TimeSpan LockBeforeFirstMatch = TimeSpan.FromMinutes(15);

    private async Task<(DateTime lockTime, bool isLocked)> GetLockStatusAsync()
    {
        var firstMatchTime = await db.Matches.AnyAsync()
            ? await db.Matches.MinAsync(m => m.MatchDate)
            : DateTime.UtcNow.AddYears(1);
        var lockTime = firstMatchTime - LockBeforeFirstMatch;
        return (lockTime, DateTime.UtcNow >= lockTime);
    }

    private static async Task<string?> GetChampionAsync(ProdeaDbContext db)
    {
        var finalMatch = await db.Matches
            .Where(m => m.Phase == MatchPhase.Final && m.Status == MatchStatus.Finished)
            .FirstOrDefaultAsync();
        if (finalMatch?.HomeScore == null || finalMatch.HomeScore == finalMatch.AwayScore) return null;
        return finalMatch.HomeScore > finalMatch.AwayScore ? finalMatch.HomeTeam : finalMatch.AwayTeam;
    }

    private static async Task<List<string>> GetAvailableTeamsAsync(ProdeaDbContext db)
    {
        var groupMatches = await db.Matches
            .Where(m => m.Phase == MatchPhase.Group)
            .Select(m => new { m.HomeTeam, m.AwayTeam })
            .ToListAsync();
        return groupMatches
            .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
            .Where(t => t != "TBD")
            .Distinct().OrderBy(t => t).ToList();
    }

    [HttpGet]
    public async Task<ActionResult<ChampionPickStatusDto>> GetStatus()
    {
        try
        {
            var userId = CurrentUserId;
            var (lockTime, isLocked) = await GetLockStatusAsync();

            var myPick = await db.ChampionPicks
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.CountryName)
                .FirstOrDefaultAsync();

            var champion = await GetChampionAsync(db);
            var teams = await GetAvailableTeamsAsync(db);

            return Ok(new ChampionPickStatusDto(myPick, isLocked, lockTime, champion, [], teams));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitPick(SubmitChampionPickRequest request)
    {
        try
        {
            var userId = CurrentUserId;
            var (_, isLocked) = await GetLockStatusAsync();
            if (isLocked) return BadRequest(new { message = "El pick de campeón ya está cerrado." });

            var existing = await db.ChampionPicks.FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (existing != null)
            {
                existing.CountryName = request.CountryName;
                existing.PickedAt = DateTime.UtcNow;
            }
            else
            {
                db.ChampionPicks.Add(new ChampionPick { UserId = userId, CountryName = request.CountryName });
            }

            await db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}
