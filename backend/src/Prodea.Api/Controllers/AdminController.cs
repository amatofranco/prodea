using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(ProdeaDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("seed-fixture")]
    public async Task<IActionResult> SeedFixture()
    {
        if (!env.IsDevelopment())
            return Forbid();

        if (await db.Matches.AnyAsync())
            return Conflict(new { message = "Fixture ya cargado" });

        var matches = WorldCup2026Seed.GetGroupStageMatches();
        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();

        return Ok(new { message = $"{matches.Count} partidos cargados" });
    }

    [HttpGet("polling-status")]
    [Authorize]
    public async Task<IActionResult> GetPollingStatus()
    {
        var inProgress = await db.Matches
            .Where(m => m.Status == Models.MatchStatus.InProgress)
            .CountAsync();

        var upcoming = await db.Matches
            .Where(m => m.Status == Models.MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddHours(1))
            .CountAsync();

        return Ok(new { inProgressMatches = inProgress, upcomingInNextHour = upcoming });
    }
}
