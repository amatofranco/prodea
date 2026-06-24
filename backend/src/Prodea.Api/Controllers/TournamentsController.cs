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
[Route("api/[controller]")]
[Authorize]
public class TournamentsController(ProdeaDbContext db) : ControllerBase
{
    private const int MaxTournamentsPerUser = 10;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        try
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

        var points = await db.Predictions
            .Where(p => participantIds.Contains(p.UserId) && p.Match.MatchDate >= tournament.StartingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .ToListAsync();

        var championPicksList = await db.ChampionPicks
            .Where(cp => participantIds.Contains(cp.UserId))
            .ToListAsync();
        var championPoints = championPicksList
            .GroupBy(cp => cp.UserId)
            .ToDictionary(g => g.Key, g => g.Max(cp => cp.PointsEarned));

        var lastBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == id && mb.Phase != "")
            .GroupBy(mb => mb.UserId)
            .Select(g => g.OrderByDescending(mb => mb.AwardedAt).First())
            .ToListAsync();

        var pointsMap = points.ToDictionary(p => p.UserId, p => p.Total);
        var badgeMap = lastBadges.ToDictionary(b => b.UserId, b => b.BadgeType.ToString());

        var ranked = tournament.Participants
            .OrderByDescending(tp => pointsMap.GetValueOrDefault(tp.UserId, 0) + championPoints.GetValueOrDefault(tp.UserId, 0))
            .Select((tp, idx) => new ParticipantDto(
                tp.UserId, tp.User.Username, FullName(tp.User), tp.User.AvatarUrl,
                pointsMap.GetValueOrDefault(tp.UserId, 0) + championPoints.GetValueOrDefault(tp.UserId, 0),
                idx + 1,
                badgeMap.GetValueOrDefault(tp.UserId)
            ))
            .ToList();

        return Ok(new TournamentDetailDto(
            tournament.Id, tournament.Name, tournament.Description, tournament.Code, tournament.InviteLink,
            tournament.AdminUserId, tournament.Admin.Username,
            ranked, tournament.CreatedAt, tournament.StartingMatchDate
        ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, type = ex.GetType().Name, inner = ex.InnerException?.Message, step = "GetTournament" });
        }
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
            await new BadgeService(db).RecalculateAllBadgesAsync(id);

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
      try {
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

        var points = await db.Predictions
            .Where(p => participantIds.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .ToListAsync();

        var champPicksList2 = await db.ChampionPicks
            .Where(cp => participantIds.Contains(cp.UserId))
            .ToListAsync();
        var championPoints = champPicksList2
            .GroupBy(cp => cp.UserId)
            .ToDictionary(g => g.Key, g => g.Max(cp => cp.PointsEarned));

        var lastBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == id && mb.Phase != "")
            .GroupBy(mb => mb.UserId)
            .Select(g => g.OrderByDescending(mb => mb.AwardedAt).First())
            .ToListAsync();

        var pointsMap = points.ToDictionary(p => p.UserId, p => p.Total);
        var badgeMap = lastBadges.ToDictionary(b => b.UserId, b => b);

        return Ok(participants
            .OrderByDescending(tp => pointsMap.GetValueOrDefault(tp.UserId, 0) + championPoints.GetValueOrDefault(tp.UserId, 0))
            .Select((tp, idx) =>
            {
                var badge = badgeMap.GetValueOrDefault(tp.UserId);
                return new LeaderboardEntryDto(
                    idx + 1, tp.UserId, tp.User.Username, FullName(tp.User), tp.User.AvatarUrl,
                    pointsMap.GetValueOrDefault(tp.UserId, 0) + championPoints.GetValueOrDefault(tp.UserId, 0),
                    badge?.BadgeType.ToString(),
                    badge != null ? BadgeService.GetEmoji(badge.BadgeType) : null
                );
            })
            .ToList());
      }
      catch (Exception ex)
      {
          return StatusCode(500, new { message = ex.Message, type = ex.GetType().Name, inner = ex.InnerException?.Message, step = "GetLeaderboard" });
      }
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

        // Cantidad de exactos por jugador y fecha — solo se usa para desempatar Cracks
        // con el mismo puntaje dentro de la misma fecha.
        var exactCounts = await db.Predictions
            .Where(p => candidateIds.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate && p.PointsEarned == 3)
            .Select(p => new { p.UserId, p.Match.Phase, p.Match.Matchday })
            .ToListAsync();
        var exactCountMap = exactCounts
            .GroupBy(p => (p.UserId, Phase: p.Phase.ToString(), Matchday: p.Matchday ?? 0))
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(crackBadges
            .GroupBy(mb => new { mb.Phase, mb.Matchday })
            .Select(g =>
            {
                var winner = g
                    .OrderByDescending(mb => mb.PointsInMatchday)
                    .ThenByDescending(mb => exactCountMap.GetValueOrDefault((mb.UserId, mb.Phase, mb.Matchday)))
                    .First();
                var phase = Enum.Parse<MatchPhase>(winner.Phase);
                var label = phase switch
                {
                    MatchPhase.Group      => $"Fecha {winner.Matchday}",
                    MatchPhase.R32        => "Dieciseisavos",
                    MatchPhase.R16        => "Octavos",
                    MatchPhase.QF         => "Cuartos",
                    MatchPhase.SF         => "Semis",
                    MatchPhase.ThirdPlace => "3er Puesto",
                    MatchPhase.Final      => "Final",
                    _                     => winner.Phase,
                };
                var fullName = (winner.User.FirstName != null || winner.User.LastName != null)
                    ? $"{winner.User.FirstName} {winner.User.LastName}".Trim() : null;
                return new JornadaWinnerDto(winner.Phase, winner.Matchday, label, winner.UserId, winner.User.Username, fullName, winner.PointsInMatchday);
            })
            .OrderBy(w => w.Phase)
            .ThenBy(w => w.Matchday)
            .ToList());
    }

    [HttpGet("{id}/champion-pick")]
    public async Task<ActionResult<ChampionPickStatusDto>> GetTournamentChampionPick(int id)
    {
        try
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
            if (finalMatch?.HomeScore != null)
            {
                if (finalMatch.HomeScore > finalMatch.AwayScore) champion = finalMatch.HomeTeam;
                else if (finalMatch.AwayScore > finalMatch.HomeScore) champion = finalMatch.AwayTeam;
                else champion = finalMatch.Winner; // penalty case
            }

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
                        tp.UserId, tp.User.Username, FullName(tp.User),
                        picks.GetValueOrDefault(tp.UserId),
                        champion != null && picks.GetValueOrDefault(tp.UserId) == champion
                    ))
                    .ToList();
            }

            return Ok(new ChampionPickStatusDto(myPick, isLocked, lockTime, champion, allPicks, teams));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private static DateTime AsUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    private static string? FullName(User u) =>
        (u.FirstName != null || u.LastName != null)
            ? $"{u.FirstName} {u.LastName}".Trim()
            : null;
}
