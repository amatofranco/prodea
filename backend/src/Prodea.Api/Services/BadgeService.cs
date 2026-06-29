using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class BadgeService(ProdeaDbContext db)
{
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
    };

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

    // matchday: 1/2/3 para grupos; 0 para fases eliminatorias.
    // Devuelve los userIds que recibieron un badge nuevo (no actualizado) en este torneo,
    // para que el caller pueda deduplicar notificaciones entre torneos.
    public async Task<HashSet<int>> AssignMatchdayBadgesAsync(int tournamentId, MatchPhase phase, int matchday)
    {
        var newlyBadgedUserIds = new HashSet<int>();

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        // Con un solo participante no hay nadie contra quien compararse — no tiene
        // sentido asignar ningún mote de fecha.
        if (participants.Count <= 1) return newlyBadgedUserIds;

        var startingMatchDate = await db.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();

        var jornada = await db.Matches
            .Where(m => m.Phase == phase && (matchday == 0 ? m.Matchday == null : m.Matchday == matchday)
                && m.MatchDate >= startingMatchDate)
            .ToListAsync();

        if (jornada.Count == 0) return newlyBadgedUserIds;
        if (jornada.Any(m => m.Status != MatchStatus.Finished)) return newlyBadgedUserIds;

        var matchIds = jornada.Select(m => m.Id).ToList();

        var predictions = await db.Predictions
            .Where(p => participants.Contains(p.UserId) && matchIds.Contains(p.MatchId))
            .Include(p => p.Match)
            .ToListAsync();

        var playerStats = new Dictionary<int, (int TotalPoints, int ExactCount, bool HasAnyPrediction, bool AnyWinnerCorrect)>();
        var playerPredictedGoals = new Dictionary<int, int>();

        foreach (var userId in participants)
        {
            var userPreds = predictions.Where(p => p.UserId == userId).ToList();
            int totalPoints = userPreds.Sum(p => p.PointsEarned);
            int exactCount = userPreds.Count(p => p.PointsEarned == 3);
            bool anyWinnerCorrect = userPreds.Any(p => p.PointsEarned > 0);
            playerStats[userId] = (totalPoints, exactCount, userPreds.Count > 0, anyWinnerCorrect);
            playerPredictedGoals[userId] = userPreds.Sum(p => p.PredictedHomeScore + p.PredictedAwayScore);
        }

        int maxPoints = playerStats.Values.Select(s => s.TotalPoints).DefaultIfEmpty(0).Max();
        int minPoints = playerStats.Values.Where(s => s.HasAnyPrediction).Select(s => s.TotalPoints).DefaultIfEmpty(0).Min();
        var distinctPoints = playerStats.Values
            .Where(s => s.HasAnyPrediction)
            .Select(s => s.TotalPoints)
            .Distinct().OrderByDescending(p => p).ToList();
        int? secondPoints = distinctPoints.Count >= 2 ? distinctPoints[1] : (int?)null;
        int? penultimoPoints = distinctPoints.Count >= 3 ? distinctPoints[^2] : (int?)null;

        int maxPredictedGoals = playerPredictedGoals.Values.DefaultIfEmpty(0).Max();
        int goleadorCount = maxPredictedGoals > 0 ? playerPredictedGoals.Values.Count(g => g == maxPredictedGoals) : 0;
        var goalsWithPreds = playerPredictedGoals.Where(kv => playerStats.TryGetValue(kv.Key, out var s) && s.HasAnyPrediction).ToList();
        int minPredictedGoals = goalsWithPreds.Count > 0 ? goalsWithPreds.Min(kv => kv.Value) : 0;
        int rusticoCount = goalsWithPreds.Count > 0 ? goalsWithPreds.Count(kv => kv.Value == minPredictedGoals) : 0;

        var phaseStr = phase.ToString();

        foreach (var userId in participants)
        {
            var stats = playerStats.TryGetValue(userId, out var s) ? s : (TotalPoints: 0, ExactCount: 0, HasAnyPrediction: false, AnyWinnerCorrect: false);

            var badge = stats switch
            {
                { HasAnyPrediction: false }                                                            => MatchdayBadgeType.Dormido,
                { TotalPoints: var p } when p == maxPoints && maxPoints > 0 && participants.Count > 1  => MatchdayBadgeType.Crack,
                { TotalPoints: var p } when p == minPoints && participants.Count > 1                   => MatchdayBadgeType.Mufa,
                { ExactCount: >= 4 }                                                                   => MatchdayBadgeType.Adivino,
                { ExactCount: >= 3 }                                                                   => MatchdayBadgeType.Francotirador,
                _ when secondPoints.HasValue && stats.TotalPoints == secondPoints.Value => MatchdayBadgeType.PechoFrio,
                _ when goleadorCount == 1 && playerPredictedGoals.GetValueOrDefault(userId) == maxPredictedGoals => MatchdayBadgeType.Goleador,
                _ when rusticoCount == 1 && playerPredictedGoals.GetValueOrDefault(userId) == minPredictedGoals  => MatchdayBadgeType.Rustico,
                _ when penultimoPoints.HasValue && stats.TotalPoints == penultimoPoints.Value => MatchdayBadgeType.Tambaleante,
                { AnyWinnerCorrect: false }                                                            => MatchdayBadgeType.Payaso,
                _                                                                                      => MatchdayBadgeType.Tibio,
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
                newlyBadgedUserIds.Add(userId);
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

        return newlyBadgedUserIds;
    }

    // userTournamentMap: userId -> tournamentId a usar para el deep link de la notificación
    // (un usuario puede estar en varios torneos; se manda una sola notificación igual).
    public Task SendCardNotificationsPublicAsync(MatchPhase phase, int matchday, Dictionary<int, int> userTournamentMap, PushNotificationService push)
        => SendCardNotificationsAsync(phase, matchday, userTournamentMap, push);

    // Recalcula todos los motes del torneo desde cero — usar cuando cambia StartingMatchDate,
    // ya que los motes ya asignados no se actualizan solos al mover el corte de fechas.
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

    private static readonly MatchdayBadgeType[] PodiumTypes =
        [MatchdayBadgeType.Campeon, MatchdayBadgeType.Subcampeon, MatchdayBadgeType.TercerPuesto];

    // Se llama al terminar la Final del Mundial: calcula la tabla general final del torneo
    // (misma fórmula que el leaderboard) y pisa el MatchdayBadge de la fecha "Final" del podio
    // con Campeón/Subcampeón/Tercer puesto, reemplazando el mote que les hubiera tocado por
    // su desempeño puntual en ese partido.
    public async Task AwardTournamentResultBadgesAsync(int tournamentId)
    {
        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        if (participants.Count <= 1) return;

        var startingMatchDate = await db.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => t.StartingMatchDate)
            .FirstOrDefaultAsync();

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

        // Último/Penúltimo del torneo: solo si no se solapan con el podio (3 primeros).
        if (ranking.Count >= 4)
            await OverrideFinalBadge(ranking[^1], MatchdayBadgeType.Ultimo);
        if (ranking.Count >= 5)
            await OverrideFinalBadge(ranking[^2], MatchdayBadgeType.Penultimo);

        // Goleador/Rústico del torneo: goles predichos en total (todas las fechas), solo si
        // hay un único dueño del máximo/mínimo y no tiene ya un mote de puntos más importante.
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

    // userTournamentMap: userId -> tournamentId a usar para el deep link. Un solo push por
    // usuario aunque esté en varios torneos que terminaron la misma fecha/fase.
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

        bool tournamentFinished = !await db.Matches
            .AnyAsync(m => m.Status != MatchStatus.Finished);

        foreach (var userId in participants)
        {
            var userBadges = allBadges.Where(b => b.UserId == userId).OrderBy(b => b.AwardedAt).ToList();
            var dormidoCount = userBadges.Count(b => b.BadgeType == MatchdayBadgeType.Dormido);

            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.ElFantasma, dormidoCount > 3);

            bool rachaInfernal = userBadges.Count >= 3 &&
                userBadges.TakeLast(3).All(b => b.BadgeType == MatchdayBadgeType.Crack);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.RachaInfernal, rachaInfernal);

            bool neverLast = tournamentFinished && !userBadges.Any(b => b.BadgeType == MatchdayBadgeType.Mufa);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.ElMuro, neverLast);

            bool enCaidaLibre = userBadges.Count >= 3 &&
                userBadges[^3].PointsInMatchday > userBadges[^2].PointsInMatchday &&
                userBadges[^2].PointsInMatchday > userBadges[^1].PointsInMatchday;
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.PecheadaTotal, enCaidaLibre);

            bool tripleMufa = userBadges.Count >= 3 &&
                userBadges.TakeLast(3).All(b => b.BadgeType == MatchdayBadgeType.Mufa);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.TripleMufa, tripleMufa);

            bool tibiezaTotal = userBadges.Count >= 3 &&
                userBadges.TakeLast(3).All(b => b.BadgeType == MatchdayBadgeType.Tibio);
            await UpsertAccumulativeBadge(tournamentId, userId, AccumulativeBadgeType.TibiezaTotal, tibiezaTotal);
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
