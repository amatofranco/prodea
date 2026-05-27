using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(
    ProdeaDbContext db,
    IWebHostEnvironment env,
    FixtureService fixtureService,
    PollingStatusService pollingStatus) : ControllerBase
{
    [HttpPost("seed-fixture")]
    public async Task<IActionResult> SeedFixture(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        [FromQuery] bool force = false)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        var (count, source) = await fixtureService.ImportAsync(force);
        if (count == 0 && source == "ya cargado")
            return Conflict(new { message = "Fixture ya cargado. Usá ?force=true para reimportar." });

        return Ok(new { message = $"{count} partidos cargados", source });
    }

    [HttpGet("polling-status")]
    public async Task<IActionResult> GetPollingStatus()
    {
        var inProgress = await db.Matches
            .Where(m => m.Status == MatchStatus.InProgress)
            .CountAsync();

        var upcoming = await db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddHours(1))
            .CountAsync();

        var staleThreshold = TimeSpan.FromMinutes(15);
        var isStale = inProgress > 0
            && (pollingStatus.LastSuccessfulPoll == null
                || DateTime.UtcNow - pollingStatus.LastSuccessfulPoll > staleThreshold);

        return Ok(new
        {
            inProgressMatches = inProgress,
            upcomingInNextHour = upcoming,
            apiAvailable = pollingStatus.ApiAvailable,
            lastSuccessfulPoll = pollingStatus.LastSuccessfulPoll,
            isStale,
        });
    }
}
