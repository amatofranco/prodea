using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class BadgeService(ProdeaDbContext db)
{
    // ── Datos estáticos (frases, emojis) ───────────────────────────────

    private static readonly Dictionary<MatchdayBadgeType, string[]> Phrases = new()
    {
        [MatchdayBadgeType.MVP] = ["Jugás a otra cosa", "¿Sos DT o qué?", "Messi te manda saludos", "El grupo te odia un poquito", "Pedí aumento en el laburo, porque acá sobrás"],
        [MatchdayBadgeType.Jinx] = ["Apostaste con el corazón, no con el cerebro", "Tus predicciones son una obra de arte... abstracto", "El VAR te hubiera dado la razón... en otro universo", "Si apostabas al revés, eras campeón", "El análisis estaba bien. El fútbol, no"],
        [MatchdayBadgeType.Sniper] = ["Más preciso que un cirujano", "Cuando apuntás, no fallás", "La mira calibrada", "El VAR te consulta a vos", "Tenés el GPS de los goles"],
        [MatchdayBadgeType.Oracle] = ["¿Bola de cristal o qué?", "Cuatro exactos. Brujo confirmado.", "Clarividencia pura y dura", "Mandame los números de la quiniela", "La selección te necesita en el cuerpo técnico"],
        [MatchdayBadgeType.Choker] = ["Llegaste hasta la puerta pero no entraste", "Tan cerca, tan lejos", "El podio te vio de afuera", "Segundo es el primero de los perdedores", "El técnico te sacó justo cuando arrancabas"],
        [MatchdayBadgeType.Goalscorer] = ["Te gustan los goles, claramente", "Predijiste un mundial con el VAR apagado", "Más goles que el Bayern Munich", "El arquero no existe en tu universo", "Fuiste al mundial a atacar"],
        [MatchdayBadgeType.Miser] = ["El arquero agradeció tus predicciones", "Con el cuchillo entre los dientes", "Bilardo estaría orgulloso", "Economista del gol", "Predijiste con el freno de mano puesto"],
        [MatchdayBadgeType.Wobbler] = ["Sobreviviste de milagro", "En zona de descenso", "Peleando la promoción", "Por lo menos no sos último", "A un paso de la derrota"],
        [MatchdayBadgeType.Clown] = ["Ni uno. Increíble.", "El fútbol te debe una explicación", "Arte del error", "¿Estabas viendo otro partido?", "Ni de casualidad"],
        [MatchdayBadgeType.Sleeper] = ["El partido arrancó. Vos, no", "Gran estrategia: no jugaste", "Apareciste menos que el árbitro en el descuento", "¿Sabías que había partido hoy?", "Estrategia audaz: no existir"],
        [MatchdayBadgeType.Lukewarm] = ["Ni frío ni caliente", "Participaste. Listo.", "El fútbol te vio pasar", "Puntos: sí. Emoción: no.", "Ni arriba ni abajo, ahí nomás"],
        [MatchdayBadgeType.Champion] = ["Ya sabés cuánto pesa la copa. ¡Felicitaciones!"],
        [MatchdayBadgeType.RunnerUp] = ["Lo importante es competir... dijo nunca nadie. Te acompañamos en el sentimiento"],
        [MatchdayBadgeType.ThirdPlace] = ["Entraste al podio. Algo es algo."],
        [MatchdayBadgeType.LastPlace] = ["Por ahí en 4 años das menos vergüenza"],
        [MatchdayBadgeType.SecondToLast] = ["Al borde del papelón... menos mal"],
        [MatchdayBadgeType.TournamentGoalscorer] = ["El optimista de los resultados"],
        [MatchdayBadgeType.TournamentMiser] = ["Campeón en austeridad"],
    };

    private static readonly Dictionary<MatchdayBadgeType, string> Emojis = new()
    {
        [MatchdayBadgeType.MVP] = "🏆",
        [MatchdayBadgeType.Jinx] = "💀",
        [MatchdayBadgeType.Oracle] = "🔮",
        [MatchdayBadgeType.Sniper] = "🎯",
        [MatchdayBadgeType.Choker] = "❄️",
        [MatchdayBadgeType.Goalscorer] = "⚽",
        [MatchdayBadgeType.Miser] = "⛏️",
        [MatchdayBadgeType.Wobbler] = "🥴",
        [MatchdayBadgeType.Clown] = "🤡",
        [MatchdayBadgeType.Sleeper] = "😴",
        [MatchdayBadgeType.Lukewarm] = "🌡️",
        [MatchdayBadgeType.Champion] = "🏆",
        [MatchdayBadgeType.RunnerUp] = "🥈",
        [MatchdayBadgeType.ThirdPlace] = "🥉",
        [MatchdayBadgeType.LastPlace] = "💀",
        [MatchdayBadgeType.SecondToLast] = "🥴",
        [MatchdayBadgeType.TournamentGoalscorer] = "⚽",
        [MatchdayBadgeType.TournamentMiser] = "⛏️",
    };

    private static readonly Dictionary<AccumulativeBadgeType, string> AccumulativeEmojis = new()
    {
        [AccumulativeBadgeType.TotalChoke] = "🥶",
        [AccumulativeBadgeType.HotStreak] = "🔥",
        [AccumulativeBadgeType.TheWall] = "🧱",
        [AccumulativeBadgeType.TheGhost] = "👻",
        [AccumulativeBadgeType.TripleJinx] = "💀🔥",
        [AccumulativeBadgeType.TotalLukewarm] = "🌡️",
        [AccumulativeBadgeType.SerialGoalscorer] = "⚽",
        [AccumulativeBadgeType.TotalMiser] = "⛏️",
    };

    private static readonly (AccumulativeBadgeType Badge, MatchdayBadgeType Required)[] StreakRules =
    [
        (AccumulativeBadgeType.HotStreak, MatchdayBadgeType.MVP),
        (AccumulativeBadgeType.TripleJinx, MatchdayBadgeType.Jinx),
        (AccumulativeBadgeType.TotalChoke, MatchdayBadgeType.Choker),
        (AccumulativeBadgeType.TotalLukewarm, MatchdayBadgeType.Lukewarm),
        (AccumulativeBadgeType.SerialGoalscorer, MatchdayBadgeType.Goalscorer),
        (AccumulativeBadgeType.TotalMiser, MatchdayBadgeType.Miser),
    ];

    private static readonly MatchdayBadgeType[] PodiumTypes =
        [MatchdayBadgeType.Champion, MatchdayBadgeType.RunnerUp, MatchdayBadgeType.ThirdPlace];

    public static string GetEmoji(MatchdayBadgeType type) => Emojis[type];
    public static string GetAccumulativeEmoji(AccumulativeBadgeType type) => AccumulativeEmojis[type];

    public static string GetPhrase(MatchdayBadgeType type, int userId, int occurrenceIndex)
    {
        var options = Phrases[type];
        var indices = Enumerable.Range(0, options.Length).ToArray();
        var rng = new Random(HashCode.Combine(userId, (int)type));
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return options[indices[occurrenceIndex % options.Length]];
    }

    // ── Profile badges ──────────────────────────────────────────────────

    public async Task<(List<MatchdayBadgeDto> Matchday, List<AccumulativeBadgeDto> Accumulative)>
        GetUserProfileBadgesAsync(int tournamentId, int userId)
    {
        var badgesRaw = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == tournamentId && mb.UserId == userId && mb.Phase != "")
            .ToListAsync();

        var badges = badgesRaw
            .OrderByDescending(mb => Enum.TryParse<MatchPhase>(mb.Phase, out var p) ? (int)p : -1)
            .ThenByDescending(mb => mb.Matchday)
            .ToList();

        var occurrenceCounts = new Dictionary<MatchdayBadgeType, int>();
        var phraseIndex = badges
            .OrderBy(mb => Enum.TryParse<MatchPhase>(mb.Phase, out var p) ? (int)p : -1)
            .ThenBy(mb => mb.Matchday)
            .ToDictionary(
                mb => (mb.Phase, mb.Matchday),
                mb =>
                {
                    occurrenceCounts.TryGetValue(mb.BadgeType, out var count);
                    occurrenceCounts[mb.BadgeType] = count + 1;
                    return count;
                });

        var matchdayDtos = badges.Select(mb => new MatchdayBadgeDto(
            mb.Phase, mb.Matchday, mb.BadgeType.ToString(),
            GetEmoji(mb.BadgeType), mb.BadgeType.ToString(), mb.PointsInMatchday,
            GetPhrase(mb.BadgeType, mb.UserId, phraseIndex[(mb.Phase, mb.Matchday)]),
            mb.AwardedAt
        )).ToList();

        var accBadges = await db.AccumulativeBadges
            .Where(ab => ab.TournamentId == tournamentId && ab.UserId == userId)
            .ToListAsync();

        var accDtos = accBadges.Select(ab => new AccumulativeBadgeDto(
            ab.BadgeType.ToString(), GetAccumulativeEmoji(ab.BadgeType),
            ab.BadgeType.ToString(), ab.AwardedAt
        )).ToList();

        return (matchdayDtos, accDtos);
    }

    // ── Motes de fecha ─────────────────────────────────────────────────

    private record PlayerStats(int TotalPoints, int ExactCount, bool HasAnyPrediction, bool AnyWinnerCorrect);

    private record MatchdayContext(
        int ParticipantCount,
        int MaxPoints, int MinPoints,
        int? SecondPoints, int? PenultimoPoints,
        Dictionary<int, int> PredictedGoals,
        int MaxPredictedGoals, int GoleadorCount,
        int MinPredictedGoals, int RusticoCount);

    public async Task<HashSet<int>> AssignMatchdayBadgesAsync(int tournamentId, MatchPhase phase, int matchday)
    {
        var newlyBadgedUserIds = new HashSet<int>();

        var participants = await GetParticipantsAsync(tournamentId);
        if (participants.Count <= 1) return newlyBadgedUserIds;

        var jornada = await GetJornadaMatchesAsync(tournamentId, phase, matchday);
        if (jornada.Count == 0 || jornada.Any(m => m.Status != MatchStatus.Finished))
            return newlyBadgedUserIds;

        var matchIds = jornada.Select(m => m.Id).ToList();
        var predictions = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && matchIds.Contains(p.MatchId))
            .Include(p => p.Match)
            .ToListAsync();

        var (statsMap, goalsMap) = ComputePlayerStats(participants, predictions);
        var ctx = BuildMatchdayContext(participants.Count, statsMap, goalsMap);
        var phaseStr = phase.ToString();

        foreach (var userId in participants)
        {
            var stats = statsMap.GetValueOrDefault(userId, new(0, 0, false, false));
            var badge = DetermineBadge(userId, stats, ctx);

            bool isNew = await UpsertMatchdayBadgeAsync(
                tournamentId, userId, phaseStr, matchday, badge, stats.TotalPoints);
            if (isNew) newlyBadgedUserIds.Add(userId);
        }

        await db.SaveChangesAsync();
        await UpdateAccumulativeBadgesAsync(tournamentId);
        return newlyBadgedUserIds;
    }

    private static (Dictionary<int, PlayerStats> Stats, Dictionary<int, int> Goals) ComputePlayerStats(
        List<int> participants, List<Prediction> predictions)
    {
        var stats = new Dictionary<int, PlayerStats>();
        var goals = new Dictionary<int, int>();

        foreach (var userId in participants)
        {
            var userPreds = predictions.Where(p => p.UserId == userId).ToList();
            stats[userId] = new PlayerStats(
                TotalPoints: userPreds.Sum(p => p.PointsEarned),
                ExactCount: userPreds.Count(p => p.PointsEarned == 3),
                HasAnyPrediction: userPreds.Count > 0,
                AnyWinnerCorrect: userPreds.Any(p => p.PointsEarned > 0));
            goals[userId] = userPreds.Sum(p => p.PredictedHomeScore + p.PredictedAwayScore);
        }

        return (stats, goals);
    }

    private static MatchdayContext BuildMatchdayContext(
        int participantCount, Dictionary<int, PlayerStats> statsMap, Dictionary<int, int> goalsMap)
    {
        int maxPoints = statsMap.Values.Select(s => s.TotalPoints).DefaultIfEmpty(0).Max();
        int minPoints = statsMap.Values.Where(s => s.HasAnyPrediction)
            .Select(s => s.TotalPoints).DefaultIfEmpty(0).Min();

        var distinctPoints = statsMap.Values
            .Where(s => s.HasAnyPrediction)
            .Select(s => s.TotalPoints)
            .Distinct().OrderByDescending(p => p).ToList();
        int? secondPoints = distinctPoints.Count >= 2 ? distinctPoints[1] : null;
        int? penultimoPoints = distinctPoints.Count >= 3 ? distinctPoints[^2] : null;

        int maxGoals = goalsMap.Values.DefaultIfEmpty(0).Max();
        int goleadorCount = maxGoals > 0 ? goalsMap.Values.Count(g => g == maxGoals) : 0;

        var goalsWithPreds = goalsMap
            .Where(kv => statsMap.TryGetValue(kv.Key, out var s) && s.HasAnyPrediction).ToList();
        int minGoals = goalsWithPreds.Count > 0 ? goalsWithPreds.Min(kv => kv.Value) : 0;
        int rusticoCount = goalsWithPreds.Count > 0 ? goalsWithPreds.Count(kv => kv.Value == minGoals) : 0;

        return new MatchdayContext(participantCount, maxPoints, minPoints,
            secondPoints, penultimoPoints, goalsMap, maxGoals, goleadorCount, minGoals, rusticoCount);
    }

    private static MatchdayBadgeType DetermineBadge(int userId, PlayerStats stats, MatchdayContext ctx) => stats switch
    {
        { HasAnyPrediction: false }
            => MatchdayBadgeType.Sleeper,
        { TotalPoints: var p } when p == ctx.MaxPoints && ctx.MaxPoints > 0 && ctx.ParticipantCount > 1
            => MatchdayBadgeType.MVP,
        { TotalPoints: var p } when p == ctx.MinPoints && ctx.ParticipantCount > 1
            => MatchdayBadgeType.Jinx,
        { ExactCount: >= 4 }
            => MatchdayBadgeType.Oracle,
        { ExactCount: >= 3 }
            => MatchdayBadgeType.Sniper,
        _ when ctx.SecondPoints.HasValue && stats.TotalPoints == ctx.SecondPoints.Value
            => MatchdayBadgeType.Choker,
        _ when ctx.GoleadorCount == 1 && ctx.PredictedGoals.GetValueOrDefault(userId) == ctx.MaxPredictedGoals
            => MatchdayBadgeType.Goalscorer,
        _ when ctx.RusticoCount == 1 && ctx.PredictedGoals.GetValueOrDefault(userId) == ctx.MinPredictedGoals
            => MatchdayBadgeType.Miser,
        _ when ctx.PenultimoPoints.HasValue && stats.TotalPoints == ctx.PenultimoPoints.Value
            => MatchdayBadgeType.Wobbler,
        { AnyWinnerCorrect: false }
            => MatchdayBadgeType.Clown,
        _ => MatchdayBadgeType.Lukewarm,
    };

    private async Task<bool> UpsertMatchdayBadgeAsync(
        int tournamentId, int userId, string phase, int matchday, MatchdayBadgeType badge, int points)
    {
        var existing = await db.MatchdayBadges
            .FirstOrDefaultAsync(mb => mb.UserId == userId && mb.TournamentId == tournamentId
                && mb.Phase == phase && mb.Matchday == matchday);

        if (existing != null)
        {
            existing.BadgeType = badge;
            existing.PointsInMatchday = points;
            existing.AwardedAt = DateTime.UtcNow;
            return false;
        }

        db.MatchdayBadges.Add(new MatchdayBadge
        {
            UserId = userId,
            TournamentId = tournamentId,
            Phase = phase,
            Matchday = matchday,
            BadgeType = badge,
            PointsInMatchday = points,
        });
        return true;
    }

    // ── Recálculo ──────────────────────────────────────────────────────

    public async Task RecalculateAllBadgesAsync(int tournamentId)
    {
        await db.MatchdayBadges.Where(mb => mb.TournamentId == tournamentId).ExecuteDeleteAsync();
        await db.AccumulativeBadges.Where(ab => ab.TournamentId == tournamentId).ExecuteDeleteAsync();

        var jornadas = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Select(m => new { m.Phase, Matchday = m.Matchday ?? 0 })
            .Distinct()
            .OrderBy(j => j.Phase).ThenBy(j => j.Matchday)
            .ToListAsync();

        foreach (var j in jornadas)
            await AssignMatchdayBadgesAsync(tournamentId, j.Phase, j.Matchday);
    }

    public async Task RecalculateAccumulativeBadgesAsync(int tournamentId) =>
        await UpdateAccumulativeBadgesAsync(tournamentId);

    // ── Motes de fin de torneo ─────────────────────────────────────────

    public async Task AwardTournamentResultBadgesAsync(int tournamentId)
    {
        var participants = await GetParticipantsAsync(tournamentId);
        if (participants.Count <= 1) return;

        var (ranking, totalPointsMap) = await BuildTournamentRankingAsync(tournamentId, participants);
        var assigned = new HashSet<int>();

        for (int i = 0; i < PodiumTypes.Length && i < ranking.Count; i++)
            await OverrideFinalBadgeAsync(tournamentId, ranking[i], PodiumTypes[i], totalPointsMap, assigned);

        if (ranking.Count >= 4)
            await OverrideFinalBadgeAsync(tournamentId, ranking[^1], MatchdayBadgeType.LastPlace, totalPointsMap, assigned);
        if (ranking.Count >= 5)
            await OverrideFinalBadgeAsync(tournamentId, ranking[^2], MatchdayBadgeType.SecondToLast, totalPointsMap, assigned);

        await AwardGoalExtremesBadgesAsync(tournamentId, participants, assigned, totalPointsMap);
        await db.SaveChangesAsync();
    }

    private async Task<(List<int> Ranking, Dictionary<int, int> TotalPoints)> BuildTournamentRankingAsync(
        int tournamentId, List<int> participants)
    {
        var startingMatchDate = await GetStartingMatchDateAsync(tournamentId);

        var points = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(p => p.PointsEarned) })
            .ToListAsync();

        var championPoints = await db.ChampionPicks
            .Where(cp => participants.Contains(cp.UserId))
            .GroupBy(cp => cp.UserId)
            .Select(g => new { UserId = g.Key, Max = g.Max(cp => cp.PointsEarned) })
            .ToListAsync();

        var pointsMap = points.ToDictionary(p => p.UserId, p => p.Total);
        var champMap = championPoints.ToDictionary(c => c.UserId, c => c.Max);

        var totalPointsMap = participants.ToDictionary(
            uid => uid,
            uid => pointsMap.GetValueOrDefault(uid, 0) + champMap.GetValueOrDefault(uid, 0));

        var ranking = participants
            .OrderByDescending(uid => totalPointsMap[uid])
            .ToList();

        return (ranking, totalPointsMap);
    }

    private async Task OverrideFinalBadgeAsync(
        int tournamentId, int userId, MatchdayBadgeType type,
        Dictionary<int, int> totalPointsMap, HashSet<int> assigned)
    {
        var finalBadge = await db.MatchdayBadges.FirstOrDefaultAsync(mb =>
            mb.UserId == userId && mb.TournamentId == tournamentId && mb.Phase == "Final" && mb.Matchday == 0);
        if (finalBadge == null) return;

        finalBadge.BadgeType = type;
        finalBadge.PointsInMatchday = totalPointsMap.GetValueOrDefault(userId, 0);
        finalBadge.AwardedAt = DateTime.UtcNow;
        assigned.Add(userId);
    }

    private async Task AwardGoalExtremesBadgesAsync(
        int tournamentId, List<int> participants,
        HashSet<int> assigned, Dictionary<int, int> totalPointsMap)
    {
        var startingMatchDate = await GetStartingMatchDateAsync(tournamentId);

        var goals = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Goals = g.Sum(p => p.PredictedHomeScore + p.PredictedAwayScore) })
            .ToListAsync();

        if (goals.Count == 0) return;

        var maxGoals = goals.Max(g => g.Goals);
        var maxHolders = goals.Where(g => g.Goals == maxGoals).ToList();
        if (maxHolders.Count == 1 && !assigned.Contains(maxHolders[0].UserId))
            await OverrideFinalBadgeAsync(tournamentId, maxHolders[0].UserId, MatchdayBadgeType.TournamentGoalscorer, totalPointsMap, assigned);

        var minGoals = goals.Min(g => g.Goals);
        var minHolders = goals.Where(g => g.Goals == minGoals).ToList();
        if (minHolders.Count == 1 && !assigned.Contains(minHolders[0].UserId))
            await OverrideFinalBadgeAsync(tournamentId, minHolders[0].UserId, MatchdayBadgeType.TournamentMiser, totalPointsMap, assigned);
    }

    // ── Acumulativos ───────────────────────────────────────────────────

    private async Task UpdateAccumulativeBadgesAsync(int tournamentId)
    {
        var participants = await GetParticipantsAsync(tournamentId);

        var allBadges = await db.MatchdayBadges
            .Where(mb => mb.TournamentId == tournamentId)
            .OrderBy(mb => mb.AwardedAt)
            .ToListAsync();

        bool tournamentFinished = !await db.Matches
            .AnyAsync(m => m.Status != MatchStatus.Finished);

        foreach (var userId in participants)
        {
            var userBadges = allBadges.Where(b => b.UserId == userId).OrderBy(b => b.AwardedAt).ToList();

            foreach (var (accType, requiredType) in StreakRules)
            {
                bool hasStreak = userBadges.Count >= 3 &&
                    userBadges.TakeLast(3).All(b => b.BadgeType == requiredType);
                await UpsertAccumulativeBadge(tournamentId, userId, accType, hasStreak);
            }

            int dormidoCount = userBadges.Count(b => b.BadgeType == MatchdayBadgeType.Sleeper);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.TheGhost, dormidoCount > 3);

            bool neverLast = tournamentFinished && !userBadges.Any(b => b.BadgeType == MatchdayBadgeType.Jinx);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.TheWall, neverLast);
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

    // ── Notificaciones ─────────────────────────────────────────────────

    public Task SendCardNotificationsPublicAsync(MatchPhase phase, int matchday, Dictionary<int, int> userTournamentMap, PushNotificationService push)
        => SendCardNotificationsAsync(phase, matchday, userTournamentMap, push);

    public static string MatchdayLabel(MatchPhase phase, int matchday) => phase switch
    {
        MatchPhase.Group => $"Matchday {matchday}",
        MatchPhase.R32 => "Round of 32",
        MatchPhase.R16 => "Round of 16",
        MatchPhase.QF => "Quarter-finals",
        MatchPhase.SF => "Semi-finals",
        MatchPhase.ThirdPlace => "Third Place",
        MatchPhase.Final => "Final",
        _ => phase.ToString(),
    };

    private async Task SendCardNotificationsAsync(MatchPhase phase, int matchday, Dictionary<int, int> userTournamentMap, PushNotificationService push)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(s => userTournamentMap.Keys.Contains(s.UserId))
            .ToListAsync();

        var expired = new List<UserPushSubscription>();
        foreach (var sub in subscriptions)
        {
            try
            {
                await push.SendDataAsync(
                    sub,
                    new { type = "card", phase = phase.ToString(), matchday,
                          url = $"/tournaments/{userTournamentMap[sub.UserId]}/profile/{sub.UserId}" }
                );
            }
            catch (ExpiredSubscriptionException) { expired.Add(sub); }
            catch { /* network error — don't interrupt flow */ }
        }

        if (expired.Count > 0)
        {
            db.PushSubscriptions.RemoveRange(expired);
            await db.SaveChangesAsync();
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<List<int>> GetParticipantsAsync(int tournamentId) =>
        await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

    private async Task<DateTime> GetStartingMatchDateAsync(int tournamentId) =>
        await db.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();

    private async Task<List<Match>> GetJornadaMatchesAsync(int tournamentId, MatchPhase phase, int matchday)
    {
        var startingMatchDate = await GetStartingMatchDateAsync(tournamentId);
        return await db.Matches
            .Where(m => m.Phase == phase && (matchday == 0 ? m.Matchday == null : m.Matchday == matchday)
                && m.MatchDate >= startingMatchDate)
            .ToListAsync();
    }
}
