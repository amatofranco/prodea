using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class BadgeService(ProdeaDbContext db)
{
    // ── Datos estáticos (frases, emojis) ───────────────────────────────

    private static readonly Dictionary<MatchdayBadgeType, string[]> Phrases = new()
    {
        [MatchdayBadgeType.Crack] = ["Jugás a otra cosa", "¿Sos DT o qué?", "Messi te manda saludos", "El grupo te odia un poquito", "Pedí aumento en el laburo, porque acá sobrás"],
        [MatchdayBadgeType.Mufa] = ["Apostaste con el corazón, no con el cerebro", "Tus predicciones son una obra de arte... abstracto", "El VAR te hubiera dado la razón... en otro universo", "Si apostabas al revés, eras campeón", "El análisis estaba bien. El fútbol, no"],
        [MatchdayBadgeType.Francotirador] = ["Más preciso que un cirujano", "Cuando apuntás, no fallás", "La mira calibrada", "El VAR te consulta a vos", "Tenés el GPS de los goles"],
        [MatchdayBadgeType.Adivino] = ["¿Bola de cristal o qué?", "Cuatro exactos. Brujo confirmado.", "Clarividencia pura y dura", "Mandame los números de la quiniela", "La selección te necesita en el cuerpo técnico"],
        [MatchdayBadgeType.PechoFrio] = ["Llegaste hasta la puerta pero no entraste", "Tan cerca, tan lejos", "El podio te vio de afuera", "Segundo es el primero de los perdedores", "El técnico te sacó justo cuando arrancabas"],
        [MatchdayBadgeType.Goleador] = ["Te gustan los goles, claramente", "Predijiste un mundial con el VAR apagado", "Más goles que el Bayern Munich", "El arquero no existe en tu universo", "Fuiste al mundial a atacar"],
        [MatchdayBadgeType.Rustico] = ["El arquero agradeció tus predicciones", "Con el cuchillo entre los dientes", "Bilardo estaría orgulloso", "Economista del gol", "Predijiste con el freno de mano puesto"],
        [MatchdayBadgeType.Tambaleante] = ["Sobreviviste de milagro", "En zona de descenso", "Peleando la promoción", "Por lo menos no sos último", "A un paso de la derrota"],
        [MatchdayBadgeType.Payaso] = ["Ni uno. Increíble.", "El fútbol te debe una explicación", "Arte del error", "¿Estabas viendo otro partido?", "Ni de casualidad"],
        [MatchdayBadgeType.Dormido] = ["El partido arrancó. Vos, no", "Gran estrategia: no jugaste", "Apareciste menos que el árbitro en el descuento", "¿Sabías que había partido hoy?", "Estrategia audaz: no existir"],
        [MatchdayBadgeType.Tibio] = ["Ni frío ni caliente", "Participaste. Listo.", "El fútbol te vio pasar", "Puntos: sí. Emoción: no.", "Ni arriba ni abajo, ahí nomás"],
        [MatchdayBadgeType.Campeon] = ["Ya sabés cuánto pesa la copa. ¡Felicitaciones!"],
        [MatchdayBadgeType.Subcampeon] = ["Lo importante es competir... dijo nunca nadie. Te acompañamos en el sentimiento"],
        [MatchdayBadgeType.TercerPuesto] = ["Entraste al podio. Algo es algo."],
        [MatchdayBadgeType.Ultimo] = ["Por ahí en 4 años das menos vergüenza"],
        [MatchdayBadgeType.Penultimo] = ["Al borde del papelón... menos mal"],
        [MatchdayBadgeType.GoleadorTorneo] = ["El optimista de los resultados"],
        [MatchdayBadgeType.RusticoTorneo] = ["Campeón en austeridad"],
    };

    private static readonly Dictionary<MatchdayBadgeType, string> Emojis = new()
    {
        [MatchdayBadgeType.Crack] = "🏆",
        [MatchdayBadgeType.Mufa] = "💀",
        [MatchdayBadgeType.Adivino] = "🔮",
        [MatchdayBadgeType.Francotirador] = "🎯",
        [MatchdayBadgeType.PechoFrio] = "❄️",
        [MatchdayBadgeType.Goleador] = "⚽",
        [MatchdayBadgeType.Rustico] = "⛏️",
        [MatchdayBadgeType.Tambaleante] = "🥴",
        [MatchdayBadgeType.Payaso] = "🤡",
        [MatchdayBadgeType.Dormido] = "😴",
        [MatchdayBadgeType.Tibio] = "🌡️",
        [MatchdayBadgeType.Campeon] = "🏆",
        [MatchdayBadgeType.Subcampeon] = "🥈",
        [MatchdayBadgeType.TercerPuesto] = "🥉",
        [MatchdayBadgeType.Ultimo] = "💀",
        [MatchdayBadgeType.Penultimo] = "🥴",
        [MatchdayBadgeType.GoleadorTorneo] = "⚽",
        [MatchdayBadgeType.RusticoTorneo] = "⛏️",
    };

    private static readonly Dictionary<AccumulativeBadgeType, string> AccumulativeEmojis = new()
    {
        [AccumulativeBadgeType.PecheadaTotal] = "🥶",
        [AccumulativeBadgeType.RachaInfernal] = "🔥",
        [AccumulativeBadgeType.ElMuro] = "🧱",
        [AccumulativeBadgeType.ElFantasma] = "👻",
        [AccumulativeBadgeType.TripleMufa] = "💀🔥",
        [AccumulativeBadgeType.TibiezaTotal] = "🌡️",
        [AccumulativeBadgeType.GoleadorSerial] = "⚽",
        [AccumulativeBadgeType.RusticoTotal] = "⛏️",
    };

    private static readonly (AccumulativeBadgeType Badge, MatchdayBadgeType Required)[] StreakRules =
    [
        (AccumulativeBadgeType.RachaInfernal, MatchdayBadgeType.Crack),
        (AccumulativeBadgeType.TripleMufa, MatchdayBadgeType.Mufa),
        (AccumulativeBadgeType.PecheadaTotal, MatchdayBadgeType.PechoFrio),
        (AccumulativeBadgeType.TibiezaTotal, MatchdayBadgeType.Tibio),
        (AccumulativeBadgeType.GoleadorSerial, MatchdayBadgeType.Goleador),
        (AccumulativeBadgeType.RusticoTotal, MatchdayBadgeType.Rustico),
    ];

    private static readonly MatchdayBadgeType[] PodiumTypes =
        [MatchdayBadgeType.Campeon, MatchdayBadgeType.Subcampeon, MatchdayBadgeType.TercerPuesto];

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
            => MatchdayBadgeType.Dormido,
        { TotalPoints: var p } when p == ctx.MaxPoints && ctx.MaxPoints > 0 && ctx.ParticipantCount > 1
            => MatchdayBadgeType.Crack,
        { TotalPoints: var p } when p == ctx.MinPoints && ctx.ParticipantCount > 1
            => MatchdayBadgeType.Mufa,
        { ExactCount: >= 4 }
            => MatchdayBadgeType.Adivino,
        { ExactCount: >= 3 }
            => MatchdayBadgeType.Francotirador,
        _ when ctx.SecondPoints.HasValue && stats.TotalPoints == ctx.SecondPoints.Value
            => MatchdayBadgeType.PechoFrio,
        _ when ctx.GoleadorCount == 1 && ctx.PredictedGoals.GetValueOrDefault(userId) == ctx.MaxPredictedGoals
            => MatchdayBadgeType.Goleador,
        _ when ctx.RusticoCount == 1 && ctx.PredictedGoals.GetValueOrDefault(userId) == ctx.MinPredictedGoals
            => MatchdayBadgeType.Rustico,
        _ when ctx.PenultimoPoints.HasValue && stats.TotalPoints == ctx.PenultimoPoints.Value
            => MatchdayBadgeType.Tambaleante,
        { AnyWinnerCorrect: false }
            => MatchdayBadgeType.Payaso,
        _ => MatchdayBadgeType.Tibio,
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

    public Task SendCardNotificationsPublicAsync(MatchPhase phase, int matchday, Dictionary<int, int> userTournamentMap, PushNotificationService push)
        => SendCardNotificationsAsync(phase, matchday, userTournamentMap, push);

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

        var ranking = participants
            .OrderByDescending(uid => pointsMap.GetValueOrDefault(uid, 0) + champMap.GetValueOrDefault(uid, 0))
            .ToList();

        var assigned = new HashSet<int>();

        async Task OverrideFinalBadge(int userId, MatchdayBadgeType type)
        {
            var totalPoints = pointsMap.GetValueOrDefault(userId, 0) + champMap.GetValueOrDefault(userId, 0);

            var finalBadge = await db.MatchdayBadges.FirstOrDefaultAsync(mb =>
                mb.UserId == userId && mb.TournamentId == tournamentId && mb.Phase == "Final" && mb.Matchday == 0);
            if (finalBadge == null) return;

            finalBadge.BadgeType = type;
            finalBadge.PointsInMatchday = totalPoints;
            finalBadge.AwardedAt = DateTime.UtcNow;
            assigned.Add(userId);
        }

        for (int i = 0; i < PodiumTypes.Length && i < ranking.Count; i++)
            await OverrideFinalBadge(ranking[i], PodiumTypes[i]);

        if (ranking.Count >= 4)
            await OverrideFinalBadge(ranking[^1], MatchdayBadgeType.Ultimo);
        if (ranking.Count >= 5)
            await OverrideFinalBadge(ranking[^2], MatchdayBadgeType.Penultimo);

        var goals = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && p.Match.MatchDate >= startingMatchDate)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Goals = g.Sum(p => p.PredictedHomeScore + p.PredictedAwayScore) })
            .ToListAsync();

        if (goals.Count > 0)
        {
            var maxGoals = goals.Max(g => g.Goals);
            var maxHolders = goals.Where(g => g.Goals == maxGoals).ToList();
            if (maxHolders.Count == 1 && !assigned.Contains(maxHolders[0].UserId))
                await OverrideFinalBadge(maxHolders[0].UserId, MatchdayBadgeType.GoleadorTorneo);

            var minGoals = goals.Min(g => g.Goals);
            var minHolders = goals.Where(g => g.Goals == minGoals).ToList();
            if (minHolders.Count == 1 && !assigned.Contains(minHolders[0].UserId))
                await OverrideFinalBadge(minHolders[0].UserId, MatchdayBadgeType.RusticoTorneo);
        }

        await db.SaveChangesAsync();
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

            int dormidoCount = userBadges.Count(b => b.BadgeType == MatchdayBadgeType.Dormido);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.ElFantasma, dormidoCount > 3);

            bool neverLast = tournamentFinished && !userBadges.Any(b => b.BadgeType == MatchdayBadgeType.Mufa);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.ElMuro, neverLast);


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

    private static string JornadaLabel(MatchPhase phase, int matchday) => phase switch
    {
        MatchPhase.Group => $"Fecha {matchday}",
        MatchPhase.R32 => "Dieciseisavos",
        MatchPhase.R16 => "Octavos",
        MatchPhase.QF => "Cuartos",
        MatchPhase.SF => "Semis",
        MatchPhase.ThirdPlace => "3er Puesto",
        MatchPhase.Final => "Final",
        _ => phase.ToString(),
    };

    private async Task SendCardNotificationsAsync(MatchPhase phase, int matchday, Dictionary<int, int> userTournamentMap, PushNotificationService push)
    {
        var jornada = JornadaLabel(phase, matchday);
        var termino = phase switch
        {
            MatchPhase.Group      => $"Terminó la Fecha {matchday}.",
            MatchPhase.R32        => "Terminaron los Dieciseisavos.",
            MatchPhase.R16        => "Terminaron los Octavos.",
            MatchPhase.QF         => "Terminaron los Cuartos.",
            MatchPhase.SF         => "Terminaron las Semis.",
            MatchPhase.ThirdPlace => "Terminó el 3er Puesto.",
            MatchPhase.Final      => "Terminó la Final.",
            _                     => $"Terminó {jornada}.",
        };
        var subscriptions = await db.PushSubscriptions
            .Where(s => userTournamentMap.Keys.Contains(s.UserId))
            .ToListAsync();

        var expired = new List<UserPushSubscription>();
        foreach (var sub in subscriptions)
        {
            try
            {
                await push.SendToUserAsync(
                    sub,
                    "🃏 ¡Llegó tu Carta!",
                    $"{termino} Fijate cómo te fue y compartila.",
                    $"/torneos/{userTournamentMap[sub.UserId]}/perfil/{sub.UserId}"
                );
            }
            catch (ExpiredSubscriptionException) { expired.Add(sub); }
            catch { /* error de red — no interrumpe el flujo */ }
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
