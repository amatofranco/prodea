using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class FixtureService(
    ProdeaDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<FixtureService> logger,
    EspnBracketService espnBracket)
{
    public async Task<int> UpdateKnockoutTeamNamesAsync()
    {
        var tbdMatches = await db.Matches
            .Where(m => m.Phase != MatchPhase.Group && m.Status == MatchStatus.Scheduled
                        && (m.HomeTeam == "TBD" || m.AwayTeam == "TBD"))
            .ToListAsync();

        if (tbdMatches.Count == 0) return 0;

        int updatedFromEspn = await espnBracket.TryResolveFromEspnStandingsAsync(tbdMatches);
        int updatedFromScoreboard = await espnBracket.TryResolveFromEspnScoreboardAsync(tbdMatches);

        if (string.IsNullOrEmpty(config["FootballData:ApiKey"])) return updatedFromEspn + updatedFromScoreboard;

        try
        {
            var client = httpClientFactory.CreateClient("FootballData");
            var response = await client.GetAsync("/v4/competitions/WC/matches");
            if (!response.IsSuccessStatusCode) return updatedFromEspn;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FdMatchesResponse>(json, JsonOptions);
            if (result?.Matches == null) return updatedFromEspn;

            var apiById = result.Matches.ToDictionary(m => m.Id);
            int updated = 0;

            foreach (var match in tbdMatches)
            {
                if (match.ExternalId == null) continue;
                if (!apiById.TryGetValue(match.ExternalId.Value, out var api)) continue;

                var homeName = api.HomeTeam?.Name ?? api.HomeTeam?.ShortName;
                var awayName = api.AwayTeam?.Name ?? api.AwayTeam?.ShortName;
                if (homeName == null && awayName == null) continue;

                if (homeName != null) { match.HomeTeam = TranslateTeam(homeName); match.HomeTeamLabel = TranslateLabel(homeName); }
                if (awayName != null) { match.AwayTeam = TranslateTeam(awayName); match.AwayTeamLabel = TranslateLabel(awayName); }
                updated++;
            }

            if (updated > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Equipos knockout actualizados: {Count}", updated);
            }

            return updatedFromEspn + updatedFromScoreboard + updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al sincronizar equipos knockout");
            return updatedFromEspn + updatedFromScoreboard;
        }
    }

    public async Task<(int count, string source)> ImportAsync(bool force = false)
    {
        var hasMatches = await db.Matches.AnyAsync();
        if (hasMatches && !force) return (0, "ya cargado");

        var hasApiData = hasMatches && await db.Matches.AnyAsync(m => m.ExternalId != null);

        List<Match> incoming;
        string source;

        var apiKey = config["FootballData:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                incoming = await FetchFromApiAsync();
                source = "football-data.org";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo obtener el fixture de football-data.org, usando seed local");
                incoming = WorldCup2026Seed.GetGroupStageMatches();
                source = "seed local (fallback)";
            }
        }
        else
        {
            logger.LogWarning("FootballData:ApiKey no configurado, usando seed local");
            incoming = WorldCup2026Seed.GetGroupStageMatches();
            source = "seed local (sin API key)";
        }

        if (hasApiData)
        {
            var existingByExtId = await db.Matches
                .Where(m => m.ExternalId != null)
                .ToDictionaryAsync(m => m.ExternalId!.Value);

            int nextId = await db.Matches.MaxAsync(m => m.Id) + 1;

            foreach (var m in incoming)
            {
                if (m.ExternalId.HasValue && existingByExtId.TryGetValue(m.ExternalId.Value, out var existing))
                {
                    bool homeLabelChanged = existing.HomeTeamLabel != m.HomeTeamLabel;
                    bool awayLabelChanged = existing.AwayTeamLabel != m.AwayTeamLabel;

                    if (m.HomeTeam != "TBD" || homeLabelChanged)
                        existing.HomeTeam = m.HomeTeam;
                    if (m.AwayTeam != "TBD" || awayLabelChanged)
                        existing.AwayTeam = m.AwayTeam;

                    existing.HomeTeamLabel = m.HomeTeamLabel;
                    existing.AwayTeamLabel = m.AwayTeamLabel;
                    existing.MatchDate     = m.MatchDate;
                    existing.Phase         = m.Phase;
                    existing.Matchday      = m.Matchday;
                    existing.Group         = m.Group;
                    existing.Status        = m.Status;
                    existing.HomeScore     = m.HomeScore;
                    existing.AwayScore     = m.AwayScore;
                }
                else
                {
                    m.Id = nextId++;
                    db.Matches.Add(m);
                }
            }
        }
        else
        {
            if (hasMatches)
            {
                var seed = await db.Matches.ToListAsync();
                db.Matches.RemoveRange(seed);
                await db.SaveChangesAsync();
            }
            db.Matches.AddRange(incoming);
        }

        await db.SaveChangesAsync();
        return (incoming.Count, source);
    }

    private async Task<List<Match>> FetchFromApiAsync()
    {
        var client = httpClientFactory.CreateClient("FootballData");
        var response = await client.GetAsync("/v4/competitions/WC/matches");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FdMatchesResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Respuesta vacía de football-data.org");

        var stages = result.Matches.Select(m => $"{m.Stage}(group={m.Group})").Distinct();
        logger.LogInformation("Stages API: {Stages}", string.Join(", ", stages));

        var allMatches = result.Matches.OrderBy(m => m.UtcDate).ToList();

        var groupApiMatches = allMatches
            .Where(m => m.Stage == "GROUP_STAGE" || m.Group != null)
            .ToList();
        var knockoutApiMatches = allMatches.Except(groupApiMatches).ToList();

        Dictionary<int, int> groupMatchdays;
        if (groupApiMatches.Any(m => m.Group != null))
        {
            groupMatchdays = groupApiMatches
                .Where(m => m.Group != null)
                .GroupBy(m => m.Group!)
                .SelectMany(g =>
                {
                    var sorted = g.OrderBy(m => m.UtcDate).ToList();
                    return sorted.Select((m, i) => (m.Id, Matchday: i / 2 + 1));
                })
                .ToDictionary(x => x.Id, x => x.Matchday);
        }
        else
        {
            var sorted = groupApiMatches.OrderBy(m => m.UtcDate).ToList();
            int perRound = Math.Max(1, sorted.Count / 3);
            groupMatchdays = sorted
                .Select((m, i) => (m.Id, Matchday: Math.Min(3, i / perRound + 1)))
                .ToDictionary(x => x.Id, x => x.Matchday);
        }

        var espnBracketData = await espnBracket.FetchKnockoutBracketAsync(
            knockoutApiMatches.Select(m => (m.Id, m.UtcDate)).ToList());

        var knockoutMatchNumbers = new Dictionary<int, int>();
        int r32Num = 73;
        foreach (var m in knockoutApiMatches.Where(m => MapKnockoutPhase(m.Stage) == MatchPhase.R32).OrderBy(m => m.UtcDate))
            knockoutMatchNumbers[m.Id] = r32Num++;
        var nonR32Map = Wc2026Bracket.BuildMatchNumberMap(
            knockoutApiMatches
                .Where(m => MapKnockoutPhase(m.Stage) != MatchPhase.R32)
                .Select(m => (m.Id, MapKnockoutPhase(m.Stage), m.UtcDate)));
        foreach (var kv in nonR32Map)
            knockoutMatchNumbers[kv.Key] = kv.Value;

        var matches = new List<Match>();
        int localId = 1;

        foreach (var m in allMatches)
        {
            bool isGroup = groupApiMatches.Contains(m);
            var phase = isGroup ? MatchPhase.Group : MapKnockoutPhase(m.Stage);
            int? matchday = isGroup
                ? (groupMatchdays.TryGetValue(m.Id, out var md) ? md : null)
                : null;

            string homeTeam, awayTeam;
            string? homeLabel, awayLabel;

            if (isGroup)
            {
                homeTeam = TranslateTeam(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName);
                awayTeam = TranslateTeam(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName);
                homeLabel = TranslateLabel(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName);
                awayLabel = TranslateLabel(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName);
            }
            else if (espnBracketData.TryGetValue(m.Id, out var espnData))
            {
                homeTeam = espnData.HomeTeam != "TBD" ? espnData.HomeTeam
                         : TranslateTeam(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName);
                awayTeam = espnData.AwayTeam != "TBD" ? espnData.AwayTeam
                         : TranslateTeam(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName);
                homeLabel = espnData.HomeLabel;
                awayLabel = espnData.AwayLabel;
            }
            else
            {
                homeTeam = TranslateTeam(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName);
                awayTeam = TranslateTeam(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName);
                var matchNum = knockoutMatchNumbers.TryGetValue(m.Id, out var mn) ? mn : 0;
                (homeLabel, awayLabel) = Wc2026Bracket.GetSlotLabels(matchNum);
            }

            matches.Add(new Match
            {
                Id = localId++,
                ExternalId = m.Id,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                HomeTeamLabel = homeLabel,
                AwayTeamLabel = awayLabel,
                MatchDate = m.UtcDate,
                Status = MapStatus(m.Status),
                Phase = phase,
                Matchday = matchday,
                Group = isGroup ? m.Group : null,
                HomeScore = m.Score?.FullTime?.Home,
                AwayScore = m.Score?.FullTime?.Away,
            });
        }

        return matches;
    }

    private static MatchPhase MapKnockoutPhase(string? stage) => stage switch
    {
        "LAST_32"        => MatchPhase.R32,
        "ROUND_OF_32"    => MatchPhase.R32,
        "LAST_16"        => MatchPhase.R16,
        "ROUND_OF_16"    => MatchPhase.R16,
        "QUARTER_FINALS" => MatchPhase.QF,
        "SEMI_FINALS"    => MatchPhase.SF,
        "THIRD_PLACE"    => MatchPhase.ThirdPlace,
        "FINAL"          => MatchPhase.Final,
        _                => MatchPhase.R32,
    };

    private static string? TranslateLabel(string? name)
    {
        if (name == null) return null;
        if (EspnTeamMapping.EspnToSpanish.ContainsKey(name)) return null;

        const string winner = "Winner Group ";
        if (name.StartsWith(winner, StringComparison.OrdinalIgnoreCase))
            return $"1º Grupo {name[winner.Length..].Trim()}";

        const string runnerUp = "Runner-up Group ";
        if (name.StartsWith(runnerUp, StringComparison.OrdinalIgnoreCase))
            return $"2º Grupo {name[runnerUp.Length..].Trim()}";

        const string winnerMatch = "Winner Match ";
        if (name.StartsWith(winnerMatch, StringComparison.OrdinalIgnoreCase))
            return $"G. Partido {name[winnerMatch.Length..].Trim()}";

        const string loserMatch = "Loser Match ";
        if (name.StartsWith(loserMatch, StringComparison.OrdinalIgnoreCase))
            return $"P. Partido {name[loserMatch.Length..].Trim()}";

        return null;
    }

    private static MatchStatus MapStatus(string? status) => status switch
    {
        "IN_PLAY"  => MatchStatus.InProgress,
        "PAUSED"   => MatchStatus.InProgress,
        "FINISHED" => MatchStatus.Finished,
        "AWARDED"  => MatchStatus.Finished,
        _          => MatchStatus.Scheduled,
    };

    private static string TranslateTeam(string? name)
    {
        if (name == null) return "TBD";
        return EspnTeamMapping.Map(name);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record FdMatchesResponse([property: JsonPropertyName("matches")] List<FdMatch> Matches);
    private record FdMatch(
        int Id,
        [property: JsonPropertyName("utcDate")] DateTime UtcDate,
        string? Status,
        int? Matchday,
        string? Stage,
        string? Group,
        FdTeam? HomeTeam,
        FdTeam? AwayTeam,
        FdScore? Score);
    private record FdTeam(
        string? Name,
        [property: JsonPropertyName("shortName")] string? ShortName,
        string? Tla);
    private record FdScore([property: JsonPropertyName("fullTime")] FdFullTime? FullTime);
    private record FdFullTime(int? Home, int? Away);
}
