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

                if (apiMatch.Score?.FullTime?.Home != null)
                {
                    match.HomeScore = apiMatch.Score.FullTime.Home;
                    match.AwayScore = apiMatch.Score.FullTime.Away;
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
                if (match.ExternalId.HasValue && !activeExternalIds.Contains(match.ExternalId.Value))
                {
                    await FinalizeMatchAsync(db, match, ct);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error polling football-data.org");
            pollingStatus.ApiAvailable = false;
        }
    }

    private async Task FinalizeMatchAsync(ProdeaDbContext db, Match match, CancellationToken ct)
    {
        match.Status = MatchStatus.Finished;
        await db.SaveChangesAsync(ct);

        var predictions = await db.Predictions
            .Where(p => p.MatchId == match.Id && match.HomeScore.HasValue)
            .ToListAsync(ct);

        foreach (var pred in predictions)
        {
            pred.PointsEarned = ScoringService.CalculatePoints(pred, match.HomeScore!.Value, match.AwayScore!.Value);
            pred.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await BroadcastMatchUpdateAsync(db, match, ct);

        var badgeService = new BadgeService(db);
        var tournamentIds = await db.TournamentParticipants
            .Select(tp => tp.TournamentId)
            .Distinct()
            .ToListAsync(ct);

        var matchDate = DateOnly.FromDateTime(match.MatchDate);
        foreach (var tid in tournamentIds)
            await badgeService.AssignDailyBadgesAsync(tid, matchDate);
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
    private record FootballDataMatch(int Id, FootballDataScore? Score);
    private record FootballDataScore([property: JsonPropertyName("fullTime")] FootballDataFullTime? FullTime);
    private record FootballDataFullTime(int? Home, int? Away);
}
