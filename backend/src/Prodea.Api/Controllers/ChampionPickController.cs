using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId}/champion-pick")]
[Authorize]
public class ChampionPickController(ProdeaDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly TimeSpan LockBeforeFirstMatch = TimeSpan.FromMinutes(15);

    [HttpGet]
    public async Task<ActionResult<ChampionPickStatusDto>> GetStatus(int tournamentId)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants
            .AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!isMember) return Forbid();

        var firstMatchTime = await db.Matches.MinAsync(m => m.MatchDate);
        var lockTime = firstMatchTime - LockBeforeFirstMatch;
        var isLocked = DateTime.UtcNow >= lockTime;

        var myPick = await db.ChampionPicks
            .Where(cp => cp.TournamentId == tournamentId && cp.UserId == userId)
            .Select(cp => cp.CountryName)
            .FirstOrDefaultAsync();

        // Champion is the winner of the Final match
        string? champion = null;
        var finalMatch = await db.Matches
            .Where(m => m.Phase == MatchPhase.Final && m.Status == MatchStatus.Finished)
            .FirstOrDefaultAsync();
        if (finalMatch?.HomeScore != null && finalMatch.HomeScore != finalMatch.AwayScore)
        {
            champion = finalMatch.HomeScore > finalMatch.AwayScore
                ? finalMatch.HomeTeam
                : finalMatch.AwayTeam;
        }

        // Available teams from Group phase (both home and away, confirmed only)
        var teams = await db.Matches
            .Where(m => m.Phase == MatchPhase.Group)
            .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
            .Where(t => t != "TBD")
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // All picks — visible only after lock
        List<ParticipantPickDto> allPicks = [];
        if (isLocked)
        {
            var participants = await db.TournamentParticipants
                .Where(tp => tp.TournamentId == tournamentId)
                .Include(tp => tp.User)
                .ToListAsync();

            var picks = await db.ChampionPicks
                .Where(cp => cp.TournamentId == tournamentId)
                .ToDictionaryAsync(cp => cp.UserId, cp => cp.CountryName);

            allPicks = participants
                .Select(tp => new ParticipantPickDto(
                    tp.UserId,
                    tp.User.Username,
                    picks.GetValueOrDefault(tp.UserId),
                    champion != null && picks.GetValueOrDefault(tp.UserId) == champion
                ))
                .ToList();
        }

        return Ok(new ChampionPickStatusDto(myPick, isLocked, lockTime, champion, allPicks, teams));
    }

    [HttpPost]
    public async Task<IActionResult> SubmitPick(int tournamentId, SubmitChampionPickRequest request)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants
            .AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!isMember) return Forbid();

        var firstMatchTime = await db.Matches.MinAsync(m => m.MatchDate);
        if (DateTime.UtcNow >= firstMatchTime - LockBeforeFirstMatch)
            return BadRequest(new { message = "El pick de campeón ya está cerrado." });

        var existing = await db.ChampionPicks
            .FirstOrDefaultAsync(cp => cp.TournamentId == tournamentId && cp.UserId == userId);

        if (existing != null)
        {
            existing.CountryName = request.CountryName;
            existing.PickedAt = DateTime.UtcNow;
        }
        else
        {
            db.ChampionPicks.Add(new ChampionPick
            {
                UserId = userId,
                TournamentId = tournamentId,
                CountryName = request.CountryName,
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }
}
