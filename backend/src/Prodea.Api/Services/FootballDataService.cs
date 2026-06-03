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
    private static readonly TimeSpan KnockoutSyncInterval = TimeSpan.FromHours(6);
    private DateTime _lastKnockoutSync = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FootballDataService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollInProgressMatchesAsync(stoppingToken);
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

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PollInProgressMatchesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

        var inProgressMatches = await db.Matches
            .Where(m => m.Status == MatchStatus.InProgress && m.ExternalId != null)
            .ToListAsync(ct);

        var scheduledToStart = await db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddMinutes(5))
            .ToListAsync(ct);

        if (inProgressMatches.Count == 0 && scheduledToStart.Count == 0) return;

        var client = httpClientFactory.CreateClient("FootballData");
        var apiKey = configuration["FootballData:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("FootballData API key not configured — skipping poll");
            return;
        }

        try
        {
            var response = await client.GetAsync("/v4/competitions/WC/matches?status=IN_PLAY", ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("FootballData API returned {Status}", response.StatusCode);
                pollingStatus.ApiAvailable = false;
                return;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<FootballDataMatchesResponse>(json, JsonOptions);
            if (result?.Matches == null) return;

            pollingStatus.LastSuccessfulPoll = DateTime.UtcNow;
            pollingStatus.ApiAvailable = true;

            foreach (var apiMatch in result.Matches)
            {
                var match = await db.Matches.FirstOrDefaultAsync(m => m.ExternalId == apiMatch.Id, ct);
                if (match == null) continue;

                bool changed = false;

                if (match.Status != MatchStatus.InProgress)
                {
                    match.Status = MatchStatus.InProgress;
                    changed = true;
                }

                var (liveHome, liveAway) = FinalScore(apiMatch.Score);
                if (liveHome != null)
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
                    await db.SaveChangesAsync(ct);
                    await BroadcastMatchUpdateAsync(db, match, ct);
                }
            }

            var activeExternalIds = result.Matches.Select(m => m.Id).ToHashSet();
            foreach (var match in inProgressMatches)
            {
                if (!match.ExternalId.HasValue || activeExternalIds.Contains(match.ExternalId.Value))
                    continue;

                // No está en IN_PLAY — consultamos su estado real antes de finalizar.
                // Puede estar en PAUSED (entretiempo) o aún en alargue; solo finalizamos si la API confirma FINISHED.
                try
                {
                    var matchResp = await client.GetAsync($"/v4/matches/{match.ExternalId.Value}", ct);
                    if (!matchResp.IsSuccessStatusCode) continue;

                    var matchJson = await matchResp.Content.ReadAsStringAsync(ct);
                    var matchData = JsonSerializer.Deserialize<FootballDataSingleMatch>(matchJson, JsonOptions);

                    if (matchData?.Status is "FINISHED" or "AWARDED")
                    {
                        await FinalizeMatchAsync(db, push, match, matchData.Score, ct);
                    }
                    // PAUSED, SCHEDULED, etc. → dejamos en InProgress
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error verificando estado del partido {ExternalId}", match.ExternalId);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error polling football-data.org");
            pollingStatus.ApiAvailable = false;
        }
    }

    private async Task FinalizeMatchAsync(ProdeaDbContext db, PushNotificationService push, Match match, FootballDataScore? apiScore, CancellationToken ct)
    {
        match.Status = MatchStatus.Finished;

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

    private record FootballDataMatchesResponse([property: JsonPropertyName("matches")] List<FootballDataMatch> Matches);
    private record FootballDataMatch(int Id, int? Minute, FootballDataScore? Score);
    private record FootballDataScore(
        [property: JsonPropertyName("winner")] string? Winner,
        [property: JsonPropertyName("fullTime")] FootballDataFullTime? FullTime,
        [property: JsonPropertyName("regularTime")] FootballDataFullTime? RegularTime,
        [property: JsonPropertyName("extraTime")] FootballDataFullTime? ExtraTime
    );
    private record FootballDataFullTime(int? Home, int? Away);
    private record FootballDataSingleMatch(string Status, FootballDataScore? Score);

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
