using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Prodea.Api.Data;
using Prodea.Api.Hubs;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class FootballDataService(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    IHubContext<TournamentHub> hubContext,
    ILogger<FootballDataService> logger,
    IConfiguration configuration,
    PollingStatusService pollingStatus)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LivePollingInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FastPollingInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan KnockoutSyncInterval = TimeSpan.FromHours(6);
    private const int MaxLiveStatusAttempts = 5;
    private static readonly TimeSpan LiveStatusRetryDelay = TimeSpan.FromMilliseconds(600);
    private DateTime _lastKnockoutSync = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FootballDataService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextDelay = PollingInterval;
            try
            {
                nextDelay = await PollInProgressMatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error polling football-data.org");
            }

            if (DateTime.UtcNow - _lastKnockoutSync > KnockoutSyncInterval)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var fixtureService = scope.ServiceProvider.GetRequiredService<FixtureService>();
                    await fixtureService.UpdateKnockoutTeamNamesAsync();
                    _lastKnockoutSync = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sincronizando equipos knockout");
                }
            }

            await Task.Delay(nextDelay, stoppingToken);
        }
    }

    private async Task<TimeSpan> PollInProgressMatchesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

        // El endpoint de competición (/v4/competitions/WC/matches?status=IN_PLAY) devuelve
        // datos cacheados/desactualizados durante el Mundial, así que consultamos cada
        // partido individualmente vía /v4/matches/{id}, que sí refleja el estado en vivo.
        var matchesToCheck = await db.Matches
            .Where(m => m.ExternalId != null &&
                (m.Status == MatchStatus.InProgress ||
                 (m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddMinutes(5))))
            .ToListAsync(ct);

        if (matchesToCheck.Count == 0) return PollingInterval;

        var client = httpClientFactory.CreateClient("FootballData");
        var apiKey = configuration["FootballData:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("FootballData API key not configured — skipping poll");
            return PollingInterval;
        }

        var anySuccess = false;
        var pendingKickoff = false;
        var wasRateLimited = false;

        foreach (var match in matchesToCheck)
        {
            FootballDataSingleMatch? apiMatch = null;
            var rateLimited = false;

            // football-data.org sirve respuestas de réplicas desincronizadas: una misma consulta
            // puede devolver el estado viejo (TIMED) o el real (IN_PLAY/FINISHED) según a qué
            // réplica pegue. Reintentamos unas veces hasta encontrar una respuesta "en vivo".
            for (var attempt = 1; attempt <= MaxLiveStatusAttempts; attempt++)
            {
                try
                {
                    var response = await client.GetAsync($"/v4/matches/{match.ExternalId}", ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("FootballData API returned {Status} para partido {ExternalId}", response.StatusCode, match.ExternalId);
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            rateLimited = true;
                            break;
                        }
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync(ct);
                    var current = JsonSerializer.Deserialize<FootballDataSingleMatch>(json, JsonOptions);
                    if (current == null) continue;

                    anySuccess = true;

                    // Nos quedamos con la respuesta "en vivo" más reciente (mayor lastUpdated):
                    // una réplica desincronizada puede devolver FINISHED con un marcador viejo.
                    if (current.Status is "IN_PLAY" or "PAUSED" or "FINISHED" or "AWARDED"
                        && (apiMatch == null || current.LastUpdated > apiMatch.LastUpdated))
                    {
                        apiMatch = current;
                    }
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "HTTP error consultando partido {ExternalId}", match.ExternalId);
                }

                if (attempt < MaxLiveStatusAttempts)
                    await Task.Delay(LiveStatusRetryDelay, ct);
            }

            if (rateLimited) { wasRateLimited = true; break; }
            if (apiMatch == null)
            {
                // El partido todavía no fue marcado IN_PLAY por football-data.org pese a
                // que ya debería haber arrancado (o está a punto). Volvemos a chequear pronto.
                if (match.Status == MatchStatus.Scheduled) pendingKickoff = true;
                continue;
            }

            if (apiMatch.Status is "FINISHED" or "AWARDED")
            {
                await FinalizeMatchAsync(db, push, match, apiMatch.Score, ct);
                continue;
            }

            if (apiMatch.Status is "IN_PLAY" or "PAUSED")
            {
                bool changed = false;

                if (match.Status != MatchStatus.InProgress)
                {
                    match.Status = MatchStatus.InProgress;
                    changed = true;
                }

                var (liveHome, liveAway) = FinalScore(apiMatch.Score);
                if (liveHome != null && (match.HomeScore != liveHome || match.AwayScore != liveAway))
                {
                    match.HomeScore = liveHome;
                    match.AwayScore = liveAway;
                    changed = true;
                }

                if (apiMatch.Minute != null && match.Minute != apiMatch.Minute)
                {
                    match.Minute = apiMatch.Minute;
                    changed = true;
                }

                if (changed)
                {
                    match.LastUpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    await BroadcastMatchUpdateAsync(db, match, ct);
                }
            }
            // SCHEDULED / TIMED → el partido todavía no arrancó según la API
        }

        pollingStatus.ApiAvailable = anySuccess;
        if (anySuccess) pollingStatus.LastSuccessfulPoll = DateTime.UtcNow;

        // Si nos limitaron la tasa, hacemos backoff al intervalo normal.
        if (wasRateLimited) return PollingInterval;

        // Partido por arrancar/confirmar todavía: reintentamos en 1 minuto.
        if (pendingKickoff) return FastPollingInterval;

        // Hay al menos un partido en curso confirmado: chequeamos cada 2 minutos.
        if (matchesToCheck.Any(m => m.Status == MatchStatus.InProgress)) return LivePollingInterval;

        return PollingInterval;
    }

    private async Task FinalizeMatchAsync(ProdeaDbContext db, PushNotificationService push, Match match, FootballDataScore? apiScore, CancellationToken ct)
    {
        match.Status = MatchStatus.Finished;
        match.FinishedAt = DateTime.UtcNow;
        match.LastUpdatedAt = DateTime.UtcNow;

        // apiWinner: "HOME_TEAM" | "AWAY_TEAM" | "DRAW"
        string? apiWinner = apiScore?.Winner;
        if (apiWinner == "HOME_TEAM") match.Winner = match.HomeTeam;
        else if (apiWinner == "AWAY_TEAM") match.Winner = match.AwayTeam;

        // Display score: regularTime + extraTime (shows final result including ET goals)
        var (finalHome, finalAway) = FinalScore(apiScore);
        if (finalHome != null)
        {
            match.HomeScore = finalHome;
            match.AwayScore = finalAway;
        }

        await db.SaveChangesAsync(ct);

        // Scoring se evalúa contra el score al cierre del tiempo jugado (90' o 120' si hubo alargue),
        // antes de penales. FinalScore() ya maneja ambos casos correctamente.
        var (scoredHome, scoredAway) = FinalScore(apiScore);
        int homeFinal = scoredHome ?? match.HomeScore ?? 0;
        int awayFinal = scoredAway ?? match.AwayScore ?? 0;

        // winnerSide: quién pasó de ronda — aplica solo cuando el partido terminó empatado al cierre (fue a penales)
        string? winnerSide = null;
        if (homeFinal == awayFinal && match.Winner != null)
            winnerSide = match.Winner == match.HomeTeam ? "home" : "away";

        var predictions = await db.Predictions
            .Where(p => p.MatchId == match.Id)
            .ToListAsync(ct);

        foreach (var pred in predictions)
        {
            pred.PointsEarned = ScoringService.CalculatePoints(pred, homeFinal, awayFinal, winnerSide);
            pred.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await BroadcastMatchUpdateAsync(db, match, ct);

        var badgeService = new BadgeService(db);
        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var tid in tournamentIds)
            await badgeService.AssignMatchdayBadgesAsync(tid, match.Phase, match.Matchday ?? 0, push);

        if (match.Phase == MatchPhase.Final && match.HomeScore.HasValue)
            await AwardChampionPickPointsAsync(db, match, ct);
    }

    private static async Task AwardChampionPickPointsAsync(ProdeaDbContext db, Match match, CancellationToken ct)
    {
        string? champion = null;
        if (match.HomeScore > match.AwayScore) champion = match.HomeTeam;
        else if (match.AwayScore > match.HomeScore) champion = match.AwayTeam;
        else champion = match.Winner; // penalty: Winner already set from apiWinner

        if (champion == null) return;

        var winners = await db.ChampionPicks
            .Where(cp => cp.CountryName == champion && cp.PointsEarned == 0)
            .ToListAsync(ct);
        foreach (var pick in winners) pick.PointsEarned = 10;
        await db.SaveChangesAsync(ct);
    }

    private async Task BroadcastMatchUpdateAsync(ProdeaDbContext db, Match match, CancellationToken ct)
    {
        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId)
            .Distinct()
            .ToListAsync(ct);

        var payload = new
        {
            matchId = match.Id,
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            status = match.Status.ToString(),
        };

        foreach (var tid in tournamentIds)
            await hubContext.Clients.Group($"tournament-{tid}").SendAsync("MatchUpdated", payload, ct);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record FootballDataScore(
        [property: JsonPropertyName("winner")] string? Winner,
        [property: JsonPropertyName("fullTime")] FootballDataFullTime? FullTime,
        [property: JsonPropertyName("regularTime")] FootballDataFullTime? RegularTime,
        [property: JsonPropertyName("extraTime")] FootballDataFullTime? ExtraTime
    );
    private record FootballDataFullTime(int? Home, int? Away);
    private record FootballDataSingleMatch(string Status, FootballDataScore? Score, int? Minute, DateTime LastUpdated);

    // Score definitivo antes de penales:
    //   regularTime + extraTime  (cuando hubo alargue; extraTime tiene solo los goles del alargue, no acumulativo)
    //   regularTime              (partido definido en 90')
    //   fullTime                 (fallback — partidos de liga o respuestas sin regularTime)
    // NOTA: fullTime de la API incluye goles de tanda de penales, por eso no se usa en knockout.
    private static (int? Home, int? Away) FinalScore(FootballDataScore? score)
    {
        if (score == null) return (null, null);

        if (score.RegularTime?.Home != null)
        {
            var etHome = score.ExtraTime?.Home ?? 0;
            var etAway = score.ExtraTime?.Away ?? 0;
            return (score.RegularTime.Home + etHome, score.RegularTime.Away + etAway);
        }

        return (score.FullTime?.Home, score.FullTime?.Away);
    }
}
