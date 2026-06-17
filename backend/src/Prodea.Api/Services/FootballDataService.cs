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
    private static readonly TimeSpan LivePollingInterval = TimeSpan.FromMinutes(1);
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
                logger.LogError(ex, "Error polling scores");
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

        var matchesToCheck = await db.Matches
            .Where(m => m.ExternalId != null &&
                (m.Status == MatchStatus.InProgress ||
                 (m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddMinutes(5))))
            .ToListAsync(ct);

        if (matchesToCheck.Count == 0) return PollingInterval;

        var anySuccess = false;
        var pendingKickoff = false;
        var wasRateLimited = false;

        // ESPN usa Eastern Time (EDT = UTC-4 durante el Mundial) para el parámetro de fecha.
        // Un partido a las 02:00 UTC del día 18 es el día 17 en ET → consultamos con el día ET correcto.
        var espnDates = matchesToCheck
            .Select(m => m.MatchDate.AddHours(-4).Date) // UTC → EDT
            .Distinct()
            .ToList();

        // ESPN: fuente primaria — 1-2 requests totales, sin rate limit ni demoras
        var espnEvents = new List<EspnEvent>();
        foreach (var date in espnDates)
        {
            var events = await FetchEspnScoreboardAsync(date, ct);
            espnEvents.AddRange(events);
        }

        // Fallback si ESPN no devolvió nada (desfase de fecha, etc.)
        if (espnEvents.Count == 0)
        {
            var todayEt = DateTime.UtcNow.AddHours(-4).Date;
            var todayEvents = await FetchEspnScoreboardAsync(todayEt, ct);
            espnEvents.AddRange(todayEvents);
        }

        if (espnEvents.Count > 0)
            logger.LogInformation("ESPN: {Count} eventos encontrados para {Dates}", espnEvents.Count, string.Join(", ", espnDates.Select(d => d.ToString("yyyy-MM-dd"))));

        foreach (var match in matchesToCheck)
        {
            var espnEvent = FindEspnMatch(espnEvents, match);

            if (espnEvent != null)
            {
                anySuccess = true;
                var statusName = espnEvent.Status.Type.Name;
                logger.LogDebug("ESPN match {Home} vs {Away}: {Status}", match.HomeTeam, match.AwayTeam, statusName);

                var isFinal = statusName is "STATUS_FULL_TIME" or "STATUS_FINAL" or "STATUS_FINAL_OT"
                                            or "STATUS_FINAL_PENALTIES" or "STATUS_AWARDED";
                // Cualquier status que no sea pre-partido ni final = en curso
                var isLive = !isFinal && statusName is not ("STATUS_SCHEDULED" or "STATUS_POSTPONED"
                                            or "STATUS_CANCELED" or "STATUS_ABANDONED" or "STATUS_SUSPENDED");

                if (isFinal)
                {
                    var (homeScore, awayScore, winner) = ExtractEspnScore(espnEvent, match);
                    var goals = await FetchEspnGoalsAsync(espnEvent.Id, ct);
                    if (goals.Count > 0)
                        match.GoalsJson = JsonSerializer.Serialize(goals);
                    await FinalizeMatchCoreAsync(db, push, match, homeScore, awayScore, winner, goals, ct);
                }
                else if (isLive)
                {
                    var changed = false;
                    List<GoalInfo>? goals = null;
                    var livePhase = statusName switch {
                        "STATUS_HALFTIME" => "ET",
                        _ => (string?)null
                    };
                    var minuteDisplay = livePhase == null
                        ? FormatEspnDisplayClock(espnEvent.Status.DisplayClock)
                        : null;

                    if (match.Status != MatchStatus.InProgress)
                    {
                        match.Status = MatchStatus.InProgress;
                        match.StartedAt = DateTime.UtcNow;
                        changed = true;
                        logger.LogInformation("ESPN detectó inicio: {Home} vs {Away} [{Status}]", match.HomeTeam, match.AwayTeam, statusName);
                    }

                    var (liveHome, liveAway, _) = ExtractEspnScore(espnEvent, match);
                    if (match.HomeScore != liveHome || match.AwayScore != liveAway)
                    {
                        match.HomeScore = liveHome;
                        match.AwayScore = liveAway;
                        changed = true;
                        goals = await FetchEspnGoalsAsync(espnEvent.Id, ct);
                        if (goals.Count > 0)
                            match.GoalsJson = JsonSerializer.Serialize(goals);
                    }

                    // Durante el entretiempo no actualizamos el minuto (queda en el del último gol o anterior)
                    if (statusName != "STATUS_HALFTIME")
                    {
                        var minute = ParseEspnMinute(espnEvent.Status.DisplayClock, espnEvent.Status.Period);
                        if (minute != null && match.Minute != minute)
                        {
                            match.Minute = minute;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        match.LastUpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);
                        await BroadcastMatchUpdateAsync(db, match, goals, livePhase, minuteDisplay, ct);
                    }
                }
                else if (statusName == "STATUS_SCHEDULED" && match.Status == MatchStatus.Scheduled)
                {
                    pendingKickoff = true;
                }

                continue; // ESPN lo procesó, no ir a football-data.org
            }

            // Fallback: football-data.org para partidos que ESPN no encontró
            FootballDataSingleMatch? apiMatch = null;
            var rateLimited = false;

            var client = httpClientFactory.CreateClient("FootballData");
            var apiKey = configuration["FootballData:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogWarning("FootballData API key not configured — skipping fallback");
                continue;
            }

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
                    match.StartedAt = DateTime.UtcNow;
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
                    await BroadcastMatchUpdateAsync(db, match, null, null, null, ct);
                }
            }
        }

        pollingStatus.ApiAvailable = anySuccess;
        if (anySuccess) pollingStatus.LastSuccessfulPoll = DateTime.UtcNow;

        if (wasRateLimited) return PollingInterval;
        if (pendingKickoff) return FastPollingInterval;
        if (matchesToCheck.Any(m => m.Status == MatchStatus.InProgress)) return LivePollingInterval;
        return PollingInterval;
    }

    // -------------------------------------------------------------------------
    // ESPN
    // -------------------------------------------------------------------------

    private async Task<List<EspnEvent>> FetchEspnScoreboardAsync(DateTime utcDate, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var dateStr = utcDate.ToString("yyyyMMdd");
            var response = await client.GetAsync($"/apis/site/v2/sports/soccer/fifa.world/scoreboard?dates={dateStr}", ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ESPN API returned {Status} para fecha {Date}", response.StatusCode, dateStr);
                return [];
            }
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<EspnScoreboardResponse>(json, JsonOptions);
            return result?.Events ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error consultando ESPN scoreboard");
            return [];
        }
    }

    private static EspnEvent? FindEspnMatch(IList<EspnEvent> events, Match match)
    {
        return events.FirstOrDefault(e =>
        {
            var comp = e.Competitions.FirstOrDefault();
            if (comp == null) return false;

            var homeComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "home");
            var awayComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "away");
            if (homeComp == null || awayComp == null) return false;

            var espnHome = MapEspnTeam(homeComp.Team.DisplayName);
            var espnAway = MapEspnTeam(awayComp.Team.DisplayName);

            var dateDiff = Math.Abs((e.Date - match.MatchDate).TotalHours);
            return dateDiff < 4 &&
                   string.Equals(espnHome, match.HomeTeam, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(espnAway, match.AwayTeam, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static (int Home, int Away, string? Winner) ExtractEspnScore(EspnEvent espnEvent, Match match)
    {
        var comp = espnEvent.Competitions.FirstOrDefault();
        var homeComp = comp?.Competitors.FirstOrDefault(c => c.HomeAway == "home");
        var awayComp = comp?.Competitors.FirstOrDefault(c => c.HomeAway == "away");

        int.TryParse(homeComp?.Score, out var home);
        int.TryParse(awayComp?.Score, out var away);

        string? winner = null;
        if (homeComp?.Winner == true) winner = match.HomeTeam;
        else if (awayComp?.Winner == true) winner = match.AwayTeam;

        return (home, away, winner);
    }

    // Parsea el minuto base desde el displayClock de ESPN (para guardar en DB).
    // Ejemplos: "5'" → 5, "45'+2'" → 45, "90'+14'" → 90, "0'" → null
    private static int? ParseEspnMinute(string? displayClock, int period)
    {
        if (string.IsNullOrEmpty(displayClock)) return null;
        var raw = displayClock.Split('+')[0].TrimEnd('\'').Trim();
        if (!int.TryParse(raw, out var min) || min == 0) return null;
        return min;
    }

    // Formatea el displayClock para mostrar en la UI, preservando el tiempo adicional.
    // Ejemplos: "5'" → "5'", "45'+5'" → "45+5'", "90'+14'" → "90+14'"
    private static string? FormatEspnDisplayClock(string? displayClock)
    {
        if (string.IsNullOrEmpty(displayClock)) return null;
        var formatted = displayClock.Replace("'+", "+").TrimEnd('\'');
        return formatted + "'";
    }

    private static string MapEspnTeam(string espnName) => EspnTeamMapping.Map(espnName);

    private async Task<List<GoalInfo>> FetchEspnGoalsAsync(string eventId, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var response = await client.GetAsync($"/apis/site/v2/sports/soccer/fifa.world/summary?event={eventId}", ct);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync(ct);
            var summary = JsonSerializer.Deserialize<EspnSummaryResponse>(json, JsonOptions);
            if (summary?.KeyEvents == null) return [];

            return summary.KeyEvents
                .Where(e => e.ScoringPlay && (
                    e.Type.TypeStr.StartsWith("goal", StringComparison.OrdinalIgnoreCase) ||
                    e.Type.TypeStr.StartsWith("penalty---scored", StringComparison.OrdinalIgnoreCase)))
                .Select(e => {
                    var name = e.Participants?.FirstOrDefault()?.Athlete.DisplayName ?? "?";
                    var isPen = e.Type.TypeStr.StartsWith("penalty---scored", StringComparison.OrdinalIgnoreCase);
                    return new GoalInfo(
                        Scorer: isPen ? $"{name} (pen.)" : name,
                        Team: MapEspnTeam(e.Team?.DisplayName ?? ""),
                        Minute: e.Clock?.DisplayValue ?? ""
                    );
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error obteniendo goles ESPN para evento {EventId}", eventId);
            return [];
        }
    }

    // -------------------------------------------------------------------------
    // Finalización
    // -------------------------------------------------------------------------

    // Ruta ESPN y fallback genérico: scores ya extraídos
    private async Task FinalizeMatchCoreAsync(ProdeaDbContext db, PushNotificationService push, Match match, int homeScore, int awayScore, string? winner, List<GoalInfo>? goals, CancellationToken ct)
    {
        match.Status = MatchStatus.Finished;
        match.FinishedAt = DateTime.UtcNow;
        match.LastUpdatedAt = DateTime.UtcNow;
        match.HomeScore = homeScore;
        match.AwayScore = awayScore;
        if (winner != null) match.Winner = winner;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Partido finalizado: {Home} {HS}-{AS} {Away}", match.HomeTeam, homeScore, awayScore, match.AwayTeam);

        string? winnerSide = null;
        if (homeScore == awayScore && match.Winner != null)
            winnerSide = match.Winner == match.HomeTeam ? "home" : "away";

        var predictions = await db.Predictions
            .Where(p => p.MatchId == match.Id)
            .ToListAsync(ct);

        foreach (var pred in predictions)
        {
            pred.PointsEarned = ScoringService.CalculatePoints(pred, homeScore, awayScore, winnerSide);
            pred.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await BroadcastMatchUpdateAsync(db, match, goals, null, null, ct);

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

    // Ruta football-data.org: usa FootballDataScore para extraer scores con lógica de ET/penales
    private async Task FinalizeMatchAsync(ProdeaDbContext db, PushNotificationService push, Match match, FootballDataScore? apiScore, CancellationToken ct)
    {
        string? winner = apiScore?.Winner switch
        {
            "HOME_TEAM" => match.HomeTeam,
            "AWAY_TEAM" => match.AwayTeam,
            _ => null
        };

        var (finalHome, finalAway) = FinalScore(apiScore);
        await FinalizeMatchCoreAsync(db, push, match, finalHome ?? match.HomeScore ?? 0, finalAway ?? match.AwayScore ?? 0, winner, null, ct);
    }

    private static async Task AwardChampionPickPointsAsync(ProdeaDbContext db, Match match, CancellationToken ct)
    {
        string? champion = null;
        if (match.HomeScore > match.AwayScore) champion = match.HomeTeam;
        else if (match.AwayScore > match.HomeScore) champion = match.AwayTeam;
        else champion = match.Winner;

        if (champion == null) return;

        var winners = await db.ChampionPicks
            .Where(cp => cp.CountryName == champion && cp.PointsEarned == 0)
            .ToListAsync(ct);
        foreach (var pick in winners) pick.PointsEarned = 10;
        await db.SaveChangesAsync(ct);
    }

    private async Task BroadcastMatchUpdateAsync(ProdeaDbContext db, Match match, List<GoalInfo>? goals, string? livePhase, string? minuteDisplay, CancellationToken ct)
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
            minute = match.Minute,
            minuteDisplay,
            goals = goals?.Select(g => new { scorer = g.Scorer, team = g.Team, minute = g.Minute }).ToList(),
            livePhase,
        };

        foreach (var tid in tournamentIds)
            await hubContext.Clients.Group($"tournament-{tid}").SendAsync("MatchUpdated", payload, ct);
    }

    // -------------------------------------------------------------------------
    // football-data.org DTOs
    // -------------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record FootballDataScore(
        [property: JsonPropertyName("winner")] string? Winner,
        [property: JsonPropertyName("fullTime")] FootballDataFullTime? FullTime,
        [property: JsonPropertyName("regularTime")] FootballDataFullTime? RegularTime,
        [property: JsonPropertyName("extraTime")] FootballDataFullTime? ExtraTime
    );
    private record FootballDataFullTime(int? Home, int? Away);
    private record FootballDataSingleMatch(string Status, FootballDataScore? Score, int? Minute, DateTime LastUpdated);

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

    // -------------------------------------------------------------------------
    // ESPN DTOs
    // -------------------------------------------------------------------------

    private record EspnScoreboardResponse(
        [property: JsonPropertyName("events")] List<EspnEvent> Events
    );

    private record EspnEvent(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("status")] EspnStatus Status,
        [property: JsonPropertyName("competitions")] List<EspnCompetition> Competitions
    );

    private record EspnStatus(
        [property: JsonPropertyName("clock")] double Clock,
        [property: JsonPropertyName("displayClock")] string DisplayClock,
        [property: JsonPropertyName("period")] int Period,
        [property: JsonPropertyName("type")] EspnStatusType Type
    );

    private record EspnStatusType(
        [property: JsonPropertyName("name")] string Name
    );

    private record EspnCompetition(
        [property: JsonPropertyName("competitors")] List<EspnCompetitor> Competitors
    );

    private record EspnCompetitor(
        [property: JsonPropertyName("homeAway")] string HomeAway,
        [property: JsonPropertyName("score")] string? Score,
        [property: JsonPropertyName("winner")] bool Winner,
        [property: JsonPropertyName("team")] EspnTeam Team
    );

    private record EspnTeam(
        [property: JsonPropertyName("displayName")] string DisplayName
    );

    // ESPN Summary DTOs (para goles)
    private record EspnSummaryResponse(
        [property: JsonPropertyName("keyEvents")] List<EspnKeyEvent>? KeyEvents
    );
    private record EspnKeyEvent(
        [property: JsonPropertyName("scoringPlay")] bool ScoringPlay,
        [property: JsonPropertyName("type")] EspnKeyEventType Type,
        [property: JsonPropertyName("clock")] EspnKeyEventClock? Clock,
        [property: JsonPropertyName("team")] EspnKeyEventTeam? Team,
        [property: JsonPropertyName("participants")] List<EspnKeyEventParticipant>? Participants
    );
    private record EspnKeyEventType(
        [property: JsonPropertyName("type")] string TypeStr
    );
    private record EspnKeyEventClock(
        [property: JsonPropertyName("displayValue")] string? DisplayValue
    );
    private record EspnKeyEventTeam(
        [property: JsonPropertyName("displayName")] string DisplayName
    );
    private record EspnKeyEventParticipant(
        [property: JsonPropertyName("athlete")] EspnKeyEventAthlete Athlete
    );
    private record EspnKeyEventAthlete(
        [property: JsonPropertyName("displayName")] string DisplayName
    );
    private record GoalInfo(string Scorer, string Team, string Minute);
}
