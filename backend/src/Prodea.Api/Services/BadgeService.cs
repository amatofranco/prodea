using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class BadgeService(ProdeaDbContext db)
{
    private static readonly Dictionary<MatchdayBadgeType, string[]> Phrases = new()
    {
        [MatchdayBadgeType.Crack] = ["Clarividencia pura", "¿Sos DT o qué?", "Messi te manda saludos"],
        [MatchdayBadgeType.Mufa] = ["Apostaste con el corazón, no con el cerebro", "Tus predicciones son una obra de arte... abstracto", "El VAR te hubiera dado la razón... en otro universo"],
        [MatchdayBadgeType.Francotirador] = ["Un tiro, un gol", "Cuando apuntás, no fallás", "La mira calibrada"],
        [MatchdayBadgeType.Adivino] = ["¿Bola de cristal o qué?", "Dos exactos. Brujo confirmado.", "Doble clarividencia"],
        [MatchdayBadgeType.Payaso] = ["Ni uno. Increíble.", "El fútbol te debe una explicación", "Arte del error"],
        [MatchdayBadgeType.Dormido] = ["El partido arrancó. Vos, dormido", "Gran estrategia: no jugaste", "Apareciste menos que el árbitro en el descuento"],
    };

    private static readonly Dictionary<MatchdayBadgeType, string> Emojis = new()
    {
        [MatchdayBadgeType.Crack] = "🏆",
        [MatchdayBadgeType.Mufa] = "💀",
        [MatchdayBadgeType.Adivino] = "🔮",
        [MatchdayBadgeType.Francotirador] = "🎯",
        [MatchdayBadgeType.Payaso] = "🤡",
        [MatchdayBadgeType.Dormido] = "😴",
    };

    private static readonly Dictionary<AccumulativeBadgeType, string> AccumulativeEmojis = new()
    {
        [AccumulativeBadgeType.EnCaidaLibre] = "📉",
        [AccumulativeBadgeType.RachaInfernal] = "🔥",
        [AccumulativeBadgeType.ElMuro] = "🧱",
        [AccumulativeBadgeType.ElFantasma] = "👻",
        [AccumulativeBadgeType.TripleMufa] = "💀🔥",
    };

    public static string GetEmoji(MatchdayBadgeType type) => Emojis[type];
    public static string GetAccumulativeEmoji(AccumulativeBadgeType type) => AccumulativeEmojis[type];

    public static string GetPhrase(MatchdayBadgeType type, int userId, string phase, int matchday)
    {
        var options = Phrases[type];
        var seed = HashCode.Combine(userId, phase.GetHashCode(), matchday);
        return options[new Random(seed).Next(options.Length)];
    }

    // matchday: 1/2/3 para grupos; 0 para fases eliminatorias
    public async Task AssignMatchdayBadgesAsync(int tournamentId, MatchPhase phase, int matchday)
    {
        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var jornada = await db.Matches
            .Where(m => m.Phase == phase && (matchday == 0 ? m.Matchday == null : m.Matchday == matchday))
            .ToListAsync();

        if (jornada.Count == 0) return;
        if (jornada.Any(m => m.Status != MatchStatus.Finished)) return;

        var matchIds = jornada.Select(m => m.Id).ToList();

        var predictions = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && matchIds.Contains(p.MatchId))
            .Include(p => p.Match)
            .ToListAsync();

        var playerStats = new Dictionary<int, (int TotalPoints, int ExactCount, bool HasAnyPrediction, bool AnyWinnerCorrect)>();

        foreach (var userId in participants)
        {
            var userPreds = predictions.Where(p => p.UserId == userId).ToList();
            int totalPoints = userPreds.Sum(p => p.PointsEarned);
            int exactCount = userPreds.Count(p => p.PointsEarned == 3);
            bool anyWinnerCorrect = userPreds.Any(p => p.PointsEarned > 0);
            playerStats[userId] = (totalPoints, exactCount, userPreds.Count > 0, anyWinnerCorrect);
        }

        int maxPoints = playerStats.Values.Select(s => s.TotalPoints).DefaultIfEmpty(0).Max();
        int minPoints = playerStats.Values.Select(s => s.TotalPoints).DefaultIfEmpty(0).Min();

        var phaseStr = phase.ToString();

        foreach (var userId in participants)
        {
            var stats = playerStats.TryGetValue(userId, out var s) ? s : (TotalPoints: 0, ExactCount: 0, HasAnyPrediction: false, AnyWinnerCorrect: false);

            var badge = stats switch
            {
                { HasAnyPrediction: false }                                                            => MatchdayBadgeType.Dormido,
                { TotalPoints: var p } when p == maxPoints && maxPoints > 0 && participants.Count > 1  => MatchdayBadgeType.Crack,
                { TotalPoints: 0 }                                                                     => MatchdayBadgeType.Payaso,
                { TotalPoints: var p } when p == minPoints && participants.Count > 1                   => MatchdayBadgeType.Mufa,
                { ExactCount: >= 2 }                                                                   => MatchdayBadgeType.Adivino,
                { ExactCount: >= 1 }                                                                   => MatchdayBadgeType.Francotirador,
                _                                                                                      => MatchdayBadgeType.Payaso,
            };

            var existing = await db.MatchdayBadges
                .FirstOrDefaultAsync(mb => mb.UserId == userId && mb.TournamentId == tournamentId
                    && mb.Phase == phaseStr && mb.Matchday == matchday);

            if (existing != null)
            {
                existing.BadgeType = badge;
                existing.PointsInMatchday = stats.TotalPoints;
                existing.AwardedAt = DateTime.UtcNow;
            }
            else
            {
                db.MatchdayBadges.Add(new MatchdayBadge
                {
                    UserId = userId,
                    TournamentId = tournamentId,
                    Phase = phaseStr,
                    Matchday = matchday,
                    BadgeType = badge,
                    PointsInMatchday = stats.TotalPoints,
                });
            }
        }

        await db.SaveChangesAsync();
        await UpdateAccumulativeBadgesAsync(tournamentId);
    }

    private async Task UpdateAccumulativeBadgesAsync(int tournamentId)
    {
        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var allBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == tournamentId)
            .OrderBy(mb => mb.AwardedAt)
            .ToListAsync();

        int totalJornadas = allBadges.Select(b => new { b.Phase, b.Matchday }).Distinct().Count();

        foreach (var userId in participants)
        {
            var userBadges = allBadges.Where(b => b.UserId == userId).OrderBy(b => b.AwardedAt).ToList();
            var dormidoCount = userBadges.Count(b => b.BadgeType == MatchdayBadgeType.Dormido);

            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.ElFantasma, dormidoCount > 3);

            bool rachaInfernal = userBadges.Count >= 3 &&
                userBadges.TakeLast(3).All(b => b.BadgeType == MatchdayBadgeType.Crack);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.RachaInfernal, rachaInfernal);

            bool neverLast = totalJornadas >= 3 && !userBadges.Any(b => b.BadgeType == MatchdayBadgeType.Mufa);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.ElMuro, neverLast);

            bool enCaidaLibre = userBadges.Count >= 3 &&
                userBadges[^3].PointsInMatchday > userBadges[^2].PointsInMatchday &&
                userBadges[^2].PointsInMatchday > userBadges[^1].PointsInMatchday;
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.EnCaidaLibre, enCaidaLibre);

            bool tripleMufa = userBadges.Count >= 3 &&
                userBadges.TakeLast(3).All(b => b.BadgeType == MatchdayBadgeType.Mufa);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.TripleMufa, tripleMufa);
        }

        await db.SaveChangesAsync();
    }

    private async Task UpsertAccumulativeBadge(int tournamentId, int userId, AccumulativeBadgeType type, bool condition)
    {
        var existing = await db.AccumulativeBadges
            .FirstOrDefaultAsync(ab => ab.UserId == userId && ab.TournamentId == tournamentId && ab.BadgeType == type);

        if (condition && existing == null)
        {
            db.AccumulativeBadges.Add(new AccumulativeBadge
            {
                UserId = userId,
                TournamentId = tournamentId,
                BadgeType = type,
            });
        }
        else if (!condition && existing != null)
        {
            db.AccumulativeBadges.Remove(existing);
        }
    }
}
