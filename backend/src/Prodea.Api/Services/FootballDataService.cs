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
    PollingStatusService pollingStatus,
    EspnApiClient espn)
    : BackgroundService
{
    private const string WcMatchesPath = "/v4/competitions/WC/matches";
    private const string MatchPath     = "/v4/matches/";
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
                    logger.LogError(ex, "Error syncing knockout teams");
                }
            }

            await Task.Delay(nextDelay, stoppingToken);
        }
    }

    private enum MatchPollResult { Processed, PendingKickoff, Skipped, RateLimited }

    private async Task<TimeSpan> PollInProgressMatchesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

        var matchesToCheck = await db.Matches
            .Where(m => m.ExternalId != null &&
                (m.Status == MatchStatus.InProgress ||
                 (m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddMinutes(15))))
            .ToListAsync(ct);

        if (matchesToCheck.Count == 0) return PollingInterval;

        var espnEvents = await FetchEspnEventsForMatchesAsync(matchesToCheck, ct);

        var anySuccess = false;
        var pendingKickoff = false;

        foreach (var match in matchesToCheck)
        {
            var espnEvent = EspnApiClient.FindMatch(espnEvents, match);

            if (espnEvent != null)
            {
                anySuccess = true;
                var result = await ProcessMatchViaEspnAsync(db, push, match, espnEvent, ct);
                if (result == MatchPollResult.PendingKickoff) pendingKickoff = true;
                continue;
            }

            var fbResult = await ProcessMatchViaFootballDataAsync(db, push, match, ct);
            if (fbResult == MatchPollResult.Processed) anySuccess = true;
            if (fbResult == MatchPollResult.PendingKickoff) pendingKickoff = true;
            if (fbResult == MatchPollResult.RateLimited)
            {
                pollingStatus.ApiAvailable = anySuccess;
                return PollingInterval;
            }
        }

        pollingStatus.ApiAvailable = anySuccess;
        if (anySuccess) pollingStatus.LastSuccessfulPoll = DateTime.UtcNow;

        if (pendingKickoff) return FastPollingInterval;
        if (matchesToCheck.Any(m => m.Status == MatchStatus.InProgress)) return LivePollingInterval;
        return PollingInterval;
    }

    // ── ESPN ────────────────────────────────────────────────────────────

    private async Task<List<EspnApiClient.EspnEvent>> FetchEspnEventsForMatchesAsync(List<Match> matches, CancellationToken ct)
    {
        var espnDates = matches
            .Select(m => m.MatchDate.AddHours(-4).Date)
            .Distinct()
            .ToList();

        var espnEvents = new List<EspnApiClient.EspnEvent>();
        foreach (var date in espnDates)
        {
            var events = await espn.FetchScoreboardAsync(date, ct);
            espnEvents.AddRange(events);
        }

        if (espnEvents.Count == 0)
        {
            var todayEt = DateTime.UtcNow.AddHours(-4).Date;
            var todayEvents = await espn.FetchScoreboardAsync(todayEt, ct);
            espnEvents.AddRange(todayEvents);
        }

        if (espnEvents.Count > 0)
            logger.LogInformation("ESPN: {Count} events found for {Dates}",
                espnEvents.Count, string.Join(", ", espnDates.Select(d => d.ToString("yyyy-MM-dd"))));

        return espnEvents;
    }

    private async Task<MatchPollResult> ProcessMatchViaEspnAsync(
        ProdeaDbContext db, PushNotificationService push, Match match, EspnApiClient.EspnEvent espnEvent, CancellationToken ct)
    {
        var statusName = espnEvent.Status.Type.Name;
        logger.LogDebug("ESPN match {Home} vs {Away}: {Status}", match.HomeTeam, match.AwayTeam, statusName);

        var isFinal = statusName is "STATUS_FULL_TIME" or "STATUS_FINAL" or "STATUS_FINAL_OT"
                                    or "STATUS_FINAL_PENALTIES" or "STATUS_FINAL_PEN"
                                    or "STATUS_FINAL_AGG" or "STATUS_FINAL_AET" or "STATUS_AWARDED";
        var isLive = !isFinal && statusName is not ("STATUS_SCHEDULED" or "STATUS_POSTPONED"
                                    or "STATUS_CANCELED" or "STATUS_ABANDONED" or "STATUS_SUSPENDED");

        if (isFinal)
        {
            var (homeScore, awayScore, winner, homePen, awayPen) = EspnApiClient.ExtractScore(espnEvent, match);
            var goals = await FetchGoalsWithRetryAsync(espnEvent.Id, homeScore + awayScore, ct);
            if (goals.Count > 0)
                match.GoalsJson = JsonSerializer.Serialize(goals);
            match.HomePenaltyScore = homePen;
            match.AwayPenaltyScore = awayPen;
            await FinalizeMatchCoreAsync(db, push, match, homeScore, awayScore, winner, goals, ct);
            return MatchPollResult.Processed;
        }

        if (isLive)
        {
            await UpdateLiveMatchFromEspnAsync(db, match, espnEvent, statusName, ct);
            return MatchPollResult.Processed;
        }

        if (statusName == "STATUS_SCHEDULED" && match.Status == MatchStatus.Scheduled)
            return MatchPollResult.PendingKickoff;

        return MatchPollResult.Processed;
    }

    private async Task UpdateLiveMatchFromEspnAsync(
        ProdeaDbContext db, Match match, EspnApiClient.EspnEvent espnEvent, string statusName, CancellationToken ct)
    {
        var changed = false;
        List<EspnApiClient.GoalInfo>? goals = null;
        var livePhase = statusName == "STATUS_HALFTIME" ? "ET" : null;
        var minuteDisplay = livePhase == null ? EspnApiClient.FormatDisplayClock(espnEvent.Status.DisplayClock) : null;

        if (match.Status != MatchStatus.InProgress)
        {
            match.Status = MatchStatus.InProgress;
            match.StartedAt = DateTime.UtcNow;
            changed = true;
            logger.LogInformation("ESPN detected match start: {Home} vs {Away} [{Status}]", match.HomeTeam, match.AwayTeam, statusName);
        }

        var (liveHome, liveAway, _, _, _) = EspnApiClient.ExtractScore(espnEvent, match);
        if (match.HomeScore != liveHome || match.AwayScore != liveAway)
        {
            match.HomeScore = liveHome;
            match.AwayScore = liveAway;
            changed = true;
            goals = await FetchGoalsWithRetryAsync(espnEvent.Id, liveHome + liveAway, ct);
            if (goals.Count > 0)
                match.GoalsJson = JsonSerializer.Serialize(goals);
        }

        if (match.LivePhase != livePhase)
        {
            match.LivePhase = livePhase;
            changed = true;
        }

        if (statusName != "STATUS_HALFTIME")
        {
            var minute = EspnApiClient.ParseMinute(espnEvent.Status.DisplayClock, espnEvent.Status.Period);
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

    // ── football-data.org (fallback) ────────────────────────────────────

    private async Task<MatchPollResult> ProcessMatchViaFootballDataAsync(
        ProdeaDbContext db, PushNotificationService push, Match match, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("FootballData");
        var apiKey = configuration["FootballData:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("FootballData API key not configured — skipping fallback");
            return MatchPollResult.Skipped;
        }

        FootballDataSingleMatch? apiMatch = null;

        for (var attempt = 1; attempt <= MaxLiveStatusAttempts; attempt++)
        {
            try
            {
                var response = await client.GetAsync($"{MatchPath}{match.ExternalId}", ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("FootballData API returned {Status} for match {ExternalId}", response.StatusCode, match.ExternalId);
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        return MatchPollResult.RateLimited;
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                var current = JsonSerializer.Deserialize<FootballDataSingleMatch>(json, JsonOptions);
                if (current == null) continue;

                if (current.Status is "IN_PLAY" or "PAUSED" or "FINISHED" or "AWARDED"
                    && (apiMatch == null || current.LastUpdated > apiMatch.LastUpdated))
                {
                    apiMatch = current;
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error fetching match {ExternalId}", match.ExternalId);
            }

            if (attempt < MaxLiveStatusAttempts)
                await Task.Delay(LiveStatusRetryDelay, ct);
        }

        if (apiMatch == null)
        {
            if (match.Status == MatchStatus.Scheduled) return MatchPollResult.PendingKickoff;
            return MatchPollResult.Skipped;
        }

        if (apiMatch.Status is "FINISHED" or "AWARDED")
        {
            await FinalizeMatchAsync(db, push, match, apiMatch.Score, ct);
            return MatchPollResult.Processed;
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

        return MatchPollResult.Processed;
    }

    // ESPN puede tardar unos segundos en publicar el último gol en keyEvents
    // aunque ya actualizó el marcador. Reintentamos hasta que el conteo coincida.
    private async Task<List<EspnApiClient.GoalInfo>> FetchGoalsWithRetryAsync(
        string eventId, int expectedGoals, CancellationToken ct)
    {
        var goals = await espn.FetchGoalsAsync(eventId, ct);
        if (goals.Count >= expectedGoals || expectedGoals == 0) return goals;

        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        var retry = await espn.FetchGoalsAsync(eventId, ct);
        return retry.Count >= goals.Count ? retry : goals;
    }

    // ── Finalización ────────────────────────────────────────────────────

    private async Task FinalizeMatchCoreAsync(ProdeaDbContext db, PushNotificationService push, Match match,
        int homeScore, int awayScore, string? winner, List<EspnApiClient.GoalInfo>? goals, CancellationToken ct)
    {
        match.Status = MatchStatus.Finished;
        match.FinishedAt = DateTime.UtcNow;
        match.LastUpdatedAt = DateTime.UtcNow;
        match.HomeScore = homeScore;
        match.AwayScore = awayScore;
        if (winner != null) match.Winner = winner;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Match finished: {Home} {HS}-{AS} {Away}", match.HomeTeam, homeScore, awayScore, match.AwayTeam);

        await BroadcastMatchUpdateAsync(db, match, goals, null, null, ct);

        // Force immediate knockout sync on next poll when any knockout match finishes
        if (match.Phase != MatchPhase.Group)
            _lastKnockoutSync = DateTime.MinValue;

        using var finalizationScope = scopeFactory.CreateScope();
        var finalizationService = finalizationScope.ServiceProvider.GetRequiredService<MatchFinalizationService>();
        await finalizationService.ProcessAsync(match);
    }

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

    private async Task BroadcastMatchUpdateAsync(ProdeaDbContext db, Match match, List<EspnApiClient.GoalInfo>? goals, string? livePhase, string? minuteDisplay, CancellationToken ct)
    {
        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId)
            .Distinct()
            .ToListAsync(ct);

        var broadcastGoals = goals
            ?? (match.GoalsJson != null
                ? JsonSerializer.Deserialize<List<EspnApiClient.GoalInfo>>(match.GoalsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                : null);

        var payload = new
        {
            matchId = match.Id,
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            status = match.Status.ToString(),
            minute = match.Minute,
            minuteDisplay,
            goals = broadcastGoals?.Select(g => new { scorer = g.Scorer, team = g.Team, minute = g.Minute }).ToList(),
            livePhase,
            homePenaltyScore = match.HomePenaltyScore,
            awayPenaltyScore = match.AwayPenaltyScore,
        };

        foreach (var tid in tournamentIds)
            await hubContext.Clients.Group($"tournament-{tid}").SendAsync("MatchUpdated", payload, ct);
    }

    // ── football-data.org DTOs ──────────────────────────────────────────

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
}
