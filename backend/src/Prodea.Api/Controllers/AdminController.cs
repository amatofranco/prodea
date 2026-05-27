using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(
    ProdeaDbContext db,
    IWebHostEnvironment env,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<AdminController> logger,
    PollingStatusService pollingStatus) : ControllerBase
{
    [HttpPost("seed-fixture")]
    public async Task<IActionResult> SeedFixture([FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (!env.IsDevelopment() && (expectedKey == null || adminKey != expectedKey))
            return Forbid();

        if (await db.Matches.AnyAsync())
            return Conflict(new { message = "Fixture ya cargado" });

        List<Match> matches;
        string source;

        var apiKey = config["FootballData:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                matches = await FetchFixtureFromApiAsync();
                source = "football-data.org";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo obtener el fixture de la API, usando seed hardcodeado");
                matches = WorldCup2026Seed.GetGroupStageMatches();
                source = "seed local (fallback)";
            }
        }
        else
        {
            matches = WorldCup2026Seed.GetGroupStageMatches();
            source = "seed local (sin API key)";
        }

        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();

        return Ok(new { message = $"{matches.Count} partidos cargados", source });
    }

    [HttpGet("polling-status")]
    public async Task<IActionResult> GetPollingStatus()
    {
        var inProgress = await db.Matches
            .Where(m => m.Status == MatchStatus.InProgress)
            .CountAsync();

        var upcoming = await db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled && m.MatchDate <= DateTime.UtcNow.AddHours(1))
            .CountAsync();

        var staleThreshold = TimeSpan.FromMinutes(15);
        var isStale = inProgress > 0
            && (pollingStatus.LastSuccessfulPoll == null
                || DateTime.UtcNow - pollingStatus.LastSuccessfulPoll > staleThreshold);

        return Ok(new
        {
            inProgressMatches = inProgress,
            upcomingInNextHour = upcoming,
            apiAvailable = pollingStatus.ApiAvailable,
            lastSuccessfulPoll = pollingStatus.LastSuccessfulPoll,
            isStale,
        });
    }

    private async Task<List<Match>> FetchFixtureFromApiAsync()
    {
        var client = httpClientFactory.CreateClient("FootballData");
        var response = await client.GetAsync("/v4/competitions/WC/matches");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FdMatchesResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Respuesta vacía de football-data.org");

        var matches = new List<Match>();
        int localId = 1;

        foreach (var m in result.Matches)
        {
            var phase = MapPhase(m.Stage);
            var matchday = phase == MatchPhase.Group
                ? m.Matchday
                : MapKnockoutMatchday(phase);

            matches.Add(new Match
            {
                Id = localId++,
                ExternalId = m.Id,
                HomeTeam = m.HomeTeam?.Name ?? "TBD",
                AwayTeam = m.AwayTeam?.Name ?? "TBD",
                MatchDate = m.UtcDate,
                Status = MapStatus(m.Status),
                Phase = phase,
                Matchday = matchday,
                HomeScore = m.Score?.FullTime?.Home,
                AwayScore = m.Score?.FullTime?.Away,
            });
        }

        return matches;
    }

    private static MatchPhase MapPhase(string? stage) => stage switch
    {
        "GROUP_STAGE"    => MatchPhase.Group,
        "ROUND_OF_32"    => MatchPhase.R32,
        "ROUND_OF_16"    => MatchPhase.R16,
        "QUARTER_FINALS" => MatchPhase.QF,
        "SEMI_FINALS"    => MatchPhase.SF,
        "THIRD_PLACE"    => MatchPhase.ThirdPlace,
        "FINAL"          => MatchPhase.Final,
        _                => MatchPhase.Group,
    };

    private static int MapKnockoutMatchday(MatchPhase phase) => phase switch
    {
        MatchPhase.R32        => 4,
        MatchPhase.R16        => 5,
        MatchPhase.QF         => 6,
        MatchPhase.SF         => 7,
        MatchPhase.ThirdPlace => 8,
        MatchPhase.Final      => 9,
        _                     => 4,
    };

    private static MatchStatus MapStatus(string? status) => status switch
    {
        "IN_PLAY"   => MatchStatus.InProgress,
        "PAUSED"    => MatchStatus.InProgress,
        "FINISHED"  => MatchStatus.Finished,
        "AWARDED"   => MatchStatus.Finished,
        _           => MatchStatus.Scheduled,
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record FdMatchesResponse([property: JsonPropertyName("matches")] List<FdMatch> Matches);
    private record FdMatch(
        int Id,
        [property: JsonPropertyName("utcDate")] DateTime UtcDate,
        string? Status,
        int? Matchday,
        string? Stage,
        FdTeam? HomeTeam,
        FdTeam? AwayTeam,
        FdScore? Score);
    private record FdTeam(string? Name);
    private record FdScore([property: JsonPropertyName("fullTime")] FdFullTime? FullTime);
    private record FdFullTime(int? Home, int? Away);
}
