using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Extensions;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TournamentsController(ProdeaDbContext db, BadgeService badgeService, TournamentRankingService ranking) : AuthorizedControllerBase
{
    private const int MaxTournamentsPerUser = 10;

    [HttpGet]
    public async Task<ActionResult<List<TournamentDto>>> GetMyTournaments()
    {
        var userId = CurrentUserId;
        var tournaments = await db.TournamentParticipants
            .Where(tp => tp.UserId == userId)
            .Include(tp => tp.Tournament).ThenInclude(t => t.Admin)
            .Include(tp => tp.Tournament).ThenInclude(t => t.Participants)
            .Select(tp => tp.Tournament)
            .ToListAsync();

        return Ok(tournaments.Select(t => new TournamentDto(
            t.Id, t.Name, t.Description, t.Code, t.InviteLink,
            t.AdminUserId, t.Admin.Username,
            t.Participants.Count, t.CreatedAt, t.StartingMatchDate
        )));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TournamentDetailDto>> GetTournament(int id)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants
            .AnyAsync(tp => tp.TournamentId == id && tp.UserId == userId);
        if (!isMember) return Forbid();

        var tournament = await db.Tournaments
            .Include(t => t.Admin)
            .Include(t => t.Participants).ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null) return NotFound();

        var participantIds = tournament.Participants.Select(tp => tp.UserId).ToList();
        var data = await ranking.ComputeAsync(id, participantIds, tournament.StartingMatchDate);

        var ranked = tournament.Participants
            .OrderByDescending(tp => TournamentRankingService.TotalPoints(data, tp.UserId))
            .Select((tp, idx) => new ParticipantDto(
                tp.UserId, tp.User.Username, tp.User.FullName(), tp.User.AvatarUrl,
                TournamentRankingService.TotalPoints(data, tp.UserId),
                idx + 1,
                data.LastBadgeMap.GetValueOrDefault(tp.UserId)?.BadgeType.ToString()
            ))
            .ToList();

        return Ok(new TournamentDetailDto(
            tournament.Id, tournament.Name, tournament.Description, tournament.Code, tournament.InviteLink,
            tournament.AdminUserId, tournament.Admin.Username,
            ranked, tournament.CreatedAt, tournament.StartingMatchDate
        ));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<TournamentDetailDto>> UpdateTournament(int id, UpdateTournamentRequest request)
    {
        var userId = CurrentUserId;
        var tournament = await db.Tournaments.Include(t => t.Admin).FirstOrDefaultAsync(t => t.Id == id);
        if (tournament == null) return NotFound();
        if (tournament.AdminUserId != userId) return Forbid();

        tournament.Description = request.Description?.Trim();

        var dateChanged = false;
        if (request.StartingMatchDate.HasValue)
        {
            var newDate = AsUtc(request.StartingMatchDate.Value);
            dateChanged = newDate != tournament.StartingMatchDate;
            tournament.StartingMatchDate = newDate;
        }

        await db.SaveChangesAsync();

        if (dateChanged)
            await badgeService.RecalculateAllBadgesAsync(id);

        return Ok(new TournamentDetailDto(
            tournament.Id, tournament.Name, tournament.Description, tournament.Code, tournament.InviteLink,
            tournament.AdminUserId, tournament.Admin.Username, [], tournament.CreatedAt, tournament.StartingMatchDate
        ));
    }

    [HttpPost]
    public async Task<ActionResult<TournamentDto>> CreateTournament(CreateTournamentRequest request)
    {
        var userId = CurrentUserId;

        var tournamentCount = await db.TournamentParticipants.CountAsync(tp => tp.UserId == userId);
        if (tournamentCount >= MaxTournamentsPerUser)
            return BadRequest(new { message = $"Límite alcanzado: podés estar en hasta {MaxTournamentsPerUser} torneos." });

        var code = GenerateCode();
        var inviteLink = Guid.NewGuid().ToString("N")[..12];
        var now = DateTime.UtcNow;

        var tournament = new Tournament
        {
            Name = request.Name,
            Description = request.Description?.Trim(),
            Code = code,
            InviteLink = inviteLink,
            AdminUserId = userId,
            CreatedAt = now,
            StartingMatchDate = request.StartingMatchDate.HasValue ? AsUtc(request.StartingMatchDate.Value) : now,
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        db.TournamentParticipants.Add(new TournamentParticipant
        {
            TournamentId = tournament.Id,
            UserId = userId,
        });

        await db.SaveChangesAsync();

        var admin = await db.Users.FindAsync(userId);
        return CreatedAtAction(nameof(GetTournament), new { id = tournament.Id },
            new TournamentDto(tournament.Id, tournament.Name, tournament.Description, tournament.Code,
                tournament.InviteLink, tournament.AdminUserId, admin!.Username, 1, tournament.CreatedAt, tournament.StartingMatchDate));
    }

    [HttpPost("join")]
    public async Task<ActionResult<TournamentDto>> JoinTournament(JoinTournamentRequest request)
    {
        var userId = CurrentUserId;
        var input = request.CodeOrInviteLink.Trim();

        var tournament = await db.Tournaments
            .Include(t => t.Admin)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Code == input || t.InviteLink == input);

        if (tournament == null)
            return NotFound(new { message = "Torneo no encontrado" });

        if (tournament.Participants.Any(p => p.UserId == userId))
            return Conflict(new { message = "Ya sos participante de este torneo" });

        if (tournament.Participants.Count >= 100)
            return BadRequest(new { message = "Este torneo ya alcanzó el límite de 100 participantes." });

        var tournamentCount = await db.TournamentParticipants.CountAsync(tp => tp.UserId == userId);
        if (tournamentCount >= MaxTournamentsPerUser)
            return BadRequest(new { message = $"Límite alcanzado: podés estar en hasta {MaxTournamentsPerUser} torneos." });

        db.TournamentParticipants.Add(new TournamentParticipant
        {
            TournamentId = tournament.Id,
            UserId = userId,
        });

        await db.SaveChangesAsync();

        return Ok(new TournamentDto(
            tournament.Id, tournament.Name, tournament.Description, tournament.Code, tournament.InviteLink,
            tournament.AdminUserId, tournament.Admin.Username,
            tournament.Participants.Count + 1, tournament.CreatedAt, tournament.StartingMatchDate
        ));
    }

    [HttpDelete("{id}/leave")]
    public async Task<IActionResult> LeaveTournament(int id)
    {
        var userId = CurrentUserId;

        var tournament = await db.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (tournament == null) return NotFound();

        var participant = tournament.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null) return NotFound(new { message = "No sos participante de este torneo" });

        if (tournament.AdminUserId == userId)
        {
            var others = tournament.Participants.Where(p => p.UserId != userId).OrderBy(p => p.JoinedAt).ToList();
            if (others.Count == 0)
            {
                db.Tournaments.Remove(tournament);
                await db.SaveChangesAsync();
                return Ok(new { message = "Saliste del torneo y, al ser el único participante, el torneo fue eliminado" });
            }
            tournament.AdminUserId = others[0].UserId;
        }

        db.TournamentParticipants.Remove(participant);
        await db.MatchdayBadges.Where(mb => mb.UserId == userId && mb.TournamentId == id).ExecuteDeleteAsync();
        await db.AccumulativeBadges.Where(ab => ab.UserId == userId && ab.TournamentId == id).ExecuteDeleteAsync();
        await db.SaveChangesAsync();

        return Ok(new { message = "Saliste del torneo" });
    }

    [HttpGet("{id}/leaderboard")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard(int id)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == id && tp.UserId == userId);
        if (!isMember) return Forbid();

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == id)
            .Include(tp => tp.User)
            .ToListAsync();

        var participantIds = participants.Select(tp => tp.UserId).ToList();
        var startingMatchDate = await db.Tournaments
            .Where(t => t.Id == id)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();
        var data = await ranking.ComputeAsync(id, participantIds, startingMatchDate);

        return Ok(participants
            .OrderByDescending(tp => TournamentRankingService.TotalPoints(data, tp.UserId))
            .Select((tp, idx) =>
            {
                var badge = data.LastBadgeMap.GetValueOrDefault(tp.UserId);
                return new LeaderboardEntryDto(
                    idx + 1, tp.UserId, tp.User.Username, tp.User.FullName(), tp.User.AvatarUrl,
                    TournamentRankingService.TotalPoints(data, tp.UserId),
                    badge?.BadgeType.ToString(),
                    badge != null ? BadgeService.GetEmoji(badge.BadgeType) : null
                );
            })
            .ToList());
    }

    [HttpGet("{id}/matchday-winners")]
    public async Task<ActionResult<List<JornadaWinnerDto>>> GetMatchdayWinners(int id)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == id && tp.UserId == userId);
        if (!isMember) return Forbid();

        var crackBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == id && mb.BadgeType == MatchdayBadgeType.Crack)
            .Include(mb => mb.User)
            .ToListAsync();

        var candidateIds = crackBadges.Select(mb => mb.UserId).Distinct().ToList();
        var startingMatchDate = await db.Tournaments
            .Where(t => t.Id == id)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();

        var exactCounts = await db.Predictions
            .Where(p => candidateIds.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate && p.PointsEarned == 3)
            .Select(p => new { p.UserId, p.Match.Phase, p.Match.Matchday })
            .ToListAsync();
        var exactCountMap = exactCounts
            .GroupBy(p => (p.UserId, Phase: p.Phase.ToString(), Matchday: p.Matchday ?? 0))
            .ToDictionary(g => g.Key, g => g.Count());

        var loadTimes = await db.Predictions
            .Where(p => candidateIds.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .Select(p => new { p.UserId, p.Match.Phase, p.Match.Matchday, p.UpdatedAt })
            .ToListAsync();
        var loadTimeMap = loadTimes
            .GroupBy(p => (p.UserId, Phase: p.Phase.ToString(), Matchday: p.Matchday ?? 0))
            .ToDictionary(g => g.Key, g => g.Max(p => p.UpdatedAt));

        return Ok(crackBadges
            .GroupBy(mb => new { mb.Phase, mb.Matchday })
            .Select(g =>
            {
                var winner = g
                    .OrderByDescending(mb => mb.PointsInMatchday)
                    .ThenByDescending(mb => exactCountMap.GetValueOrDefault((mb.UserId, mb.Phase, mb.Matchday)))
                    .ThenBy(mb => loadTimeMap.GetValueOrDefault((mb.UserId, mb.Phase, mb.Matchday), DateTime.MaxValue))
                    .First();
                var phase = Enum.Parse<MatchPhase>(winner.Phase);
                var label = BadgeService.JornadaLabel(phase, winner.Matchday);
                return new JornadaWinnerDto(winner.Phase, winner.Matchday, label, winner.UserId, winner.User.Username, winner.User.FullName(), winner.PointsInMatchday);
            })
            .OrderBy(w => w.Phase)
            .ThenBy(w => w.Matchday)
            .ToList());
    }

    [HttpGet("{id}/champion-pick")]
    public async Task<ActionResult<ChampionPickStatusDto>> GetTournamentChampionPick(int id)
    {
        var userId = CurrentUserId;
        var isMember = await db.TournamentParticipants
            .AnyAsync(tp => tp.TournamentId == id && tp.UserId == userId);
        if (!isMember) return Forbid();

        var firstMatchTime = await db.Matches.AnyAsync()
            ? await db.Matches.MinAsync(m => m.MatchDate)
            : DateTime.UtcNow.AddYears(1);
        var lockTime = firstMatchTime - TimeSpan.FromMinutes(15);
        var isLocked = DateTime.UtcNow >= lockTime;

        var myPick = await db.ChampionPicks
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.CountryName)
            .FirstOrDefaultAsync();

        string? champion = null;
        var finalMatch = await db.Matches
            .Where(m => m.Phase == MatchPhase.Final && m.Status == MatchStatus.Finished)
            .FirstOrDefaultAsync();
        if (finalMatch != null)
            champion = MatchResultHelper.DetermineChampion(finalMatch);

        var groupMatches = await db.Matches
            .Where(m => m.Phase == MatchPhase.Group)
            .Select(m => new { m.HomeTeam, m.AwayTeam })
            .ToListAsync();
        var teams = groupMatches
            .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
            .Where(t => t != "TBD")
            .Distinct().OrderBy(t => t).ToList();

        List<ParticipantPickDto> allPicks = [];
        if (isLocked)
        {
            var participants = await db.TournamentParticipants
                .Where(tp => tp.TournamentId == id)
                .Include(tp => tp.User)
                .ToListAsync();

            var participantUserIds = participants.Select(tp => tp.UserId).ToList();
            var picksList = await db.ChampionPicks
                .Where(cp => participantUserIds.Contains(cp.UserId))
                .ToListAsync();
            var picks = picksList
                .GroupBy(cp => cp.UserId)
                .ToDictionary(g => g.Key, g => g.First().CountryName);

            allPicks = participants
                .Select(tp => new ParticipantPickDto(
                    tp.UserId, tp.User.Username, tp.User.FullName(),
                    picks.GetValueOrDefault(tp.UserId),
                    champion != null && picks.GetValueOrDefault(tp.UserId) == champion
                ))
                .ToList();
        }

        return Ok(new ChampionPickStatusDto(myPick, isLocked, lockTime, champion, allPicks, teams));
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private static DateTime AsUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
}
