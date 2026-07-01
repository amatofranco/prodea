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
    private const string WcMatchesPath = "/v4/competitions/WC/matches";

    public async Task<int> UpdateKnockoutTeamNamesAsync()
    {
        var tbdMatches = await db.Matches
            .Where(m => m.Phase != MatchPhase.Group && m.Status == MatchStatus.Scheduled
                        && (m.HomeTeam == "TBD" || m.AwayTeam == "TBD"))
            .ToListAsync();

        if (tbdMatches.Count == 0) return 0;

        int updatedFromDb = await TryResolveFromDbResultsAsync(tbdMatches);
        int updatedFromEspn = await espnBracket.TryResolveFromEspnStandingsAsync(tbdMatches);
        int updatedFromScoreboard = await espnBracket.TryResolveFromEspnScoreboardAsync(tbdMatches);

        if (string.IsNullOrEmpty(config["FootballData:ApiKey"])) return updatedFromDb + updatedFromEspn + updatedFromScoreboard;

        try
        {
            var client = httpClientFactory.CreateClient("FootballData");
            var response = await client.GetAsync(WcMatchesPath);
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
                logger.LogInformation("Knockout teams updated: {Count}", updated);
            }

            return updatedFromDb + updatedFromEspn + updatedFromScoreboard + updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error syncing knockout teams");
            return updatedFromEspn + updatedFromScoreboard;
        }
    }

    private static readonly System.Text.RegularExpressions.Regex SlotRefRegex =
        new(@"^(?:Gan\.|W\.) P(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private async Task<int> TryResolveFromDbResultsAsync(List<Match> tbdMatches)
    {
        var referencedIds = tbdMatches
            .SelectMany(m => new[] { m.HomeTeamLabel, m.AwayTeamLabel })
            .Where(l => l != null)
            .Select(l => SlotRefRegex.Match(l!))
            .Where(rx => rx.Success)
            .Select(rx => int.Parse(rx.Groups[1].Value))
            .Distinct()
            .ToList();

        if (referencedIds.Count == 0) return 0;

        var finishedById = await db.Matches
            .Where(m => referencedIds.Contains(m.Id) && m.Status == MatchStatus.Finished)
            .ToDictionaryAsync(m => m.Id);

        if (finishedById.Count == 0) return 0;

        int updated = 0;
        foreach (var match in tbdMatches)
        {
            if (match.HomeTeam == "TBD" && TryResolveWinner(match.HomeTeamLabel, finishedById, out var home))
            {
                match.HomeTeam = home;
                match.HomeTeamLabel = null;
                updated++;
            }
            if (match.AwayTeam == "TBD" && TryResolveWinner(match.AwayTeamLabel, finishedById, out var away))
            {
                match.AwayTeam = away;
                match.AwayTeamLabel = null;
                updated++;
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Knockout teams resolved from DB results: {Count} slots updated", updated);
        }
        return updated;
    }

    private static bool TryResolveWinner(string? label, Dictionary<int, Match> finishedById, out string team)
    {
        team = "";
        if (label == null) return false;
        var rx = SlotRefRegex.Match(label);
        if (!rx.Success) return false;
        var id = int.Parse(rx.Groups[1].Value);
        if (!finishedById.TryGetValue(id, out var source)) return false;

        team = DetermineWinner(source);
        return !string.IsNullOrEmpty(team);
    }

    private static string DetermineWinner(Match m)
    {
        if (m.Winner != null) return m.Winner; // penales
        if (m.HomeScore == null || m.AwayScore == null) return "";
        if (m.HomeScore > m.AwayScore) return m.HomeTeam;
        if (m.AwayScore > m.HomeScore) return m.AwayTeam;
        return "";
    }

    public async Task<(int count, string source)> ImportAsync(bool force = false)
    {
        var hasMatches = await db.Matches.AnyAsync();
        if (hasMatches && !force) return (0, "already loaded");

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
                logger.LogWarning(ex, "Failed to fetch fixture from football-data.org, falling back to local seed");
                incoming = WorldCup2026Seed.GetGroupStageMatches();
                source = "seed local (fallback)";
            }
        }
        else
        {
            logger.LogWarning("FootballData:ApiKey not configured, using local seed");
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
        var response = await client.GetAsync(WcMatchesPath);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FdMatchesResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from football-data.org");

        var stages = result.Matches.Select(m => $"{m.Stage}(group={m.Group})").Distinct();
        logger.LogInformation("Stages API: {Stages}", string.Join(", ", stages));

        var allMatches = result.Matches.OrderBy(m => m.UtcDate).ToList();
        var groupApiMatches = allMatches.Where(m => m.Stage == "GROUP_STAGE" || m.Group != null).ToList();
        var knockoutApiMatches = allMatches.Except(groupApiMatches).ToList();

        var groupMatchdays = BuildGroupMatchdays(groupApiMatches);
        var espnBracketData = await espnBracket.FetchKnockoutBracketAsync(
            knockoutApiMatches.Select(m => (m.Id, m.UtcDate)).ToList());
        var knockoutMatchNumbers = BuildKnockoutMatchNumbers(knockoutApiMatches);

        return BuildMatchList(allMatches, groupApiMatches, groupMatchdays, espnBracketData, knockoutMatchNumbers);
    }

    private static Dictionary<int, int> BuildGroupMatchdays(List<FdMatch> groupMatches)
    {
        if (groupMatches.Any(m => m.Group != null))
        {
            return groupMatches
                .Where(m => m.Group != null)
                .GroupBy(m => m.Group!)
                .SelectMany(g =>
                {
                    var sorted = g.OrderBy(m => m.UtcDate).ToList();
                    return sorted.Select((m, i) => (m.Id, Matchday: i / 2 + 1));
                })
                .ToDictionary(x => x.Id, x => x.Matchday);
        }

        var ordered = groupMatches.OrderBy(m => m.UtcDate).ToList();
        int perRound = Math.Max(1, ordered.Count / 3);
        return ordered
            .Select((m, i) => (m.Id, Matchday: Math.Min(3, i / perRound + 1)))
            .ToDictionary(x => x.Id, x => x.Matchday);
    }

    private static Dictionary<int, int> BuildKnockoutMatchNumbers(List<FdMatch> knockoutMatches)
    {
        var numbers = new Dictionary<int, int>();
        int r32Num = 73;
        foreach (var m in knockoutMatches.Where(m => MapKnockoutPhase(m.Stage) == MatchPhase.R32).OrderBy(m => m.UtcDate))
            numbers[m.Id] = r32Num++;
        var nonR32Map = Wc2026Bracket.BuildMatchNumberMap(
            knockoutMatches
                .Where(m => MapKnockoutPhase(m.Stage) != MatchPhase.R32)
                .Select(m => (m.Id, MapKnockoutPhase(m.Stage), m.UtcDate)));
        foreach (var kv in nonR32Map)
            numbers[kv.Key] = kv.Value;
        return numbers;
    }

    private static List<Match> BuildMatchList(
        List<FdMatch> allMatches,
        List<FdMatch> groupMatches,
        Dictionary<int, int> groupMatchdays,
        Dictionary<int, EspnBracketService.EspnBracketData> espnBracketData,
        Dictionary<int, int> knockoutMatchNumbers)
    {
        var matches = new List<Match>();
        int localId = 1;

        foreach (var m in allMatches)
        {
            bool isGroup = groupMatches.Contains(m);
            var phase = isGroup ? MatchPhase.Group : MapKnockoutPhase(m.Stage);
            int? matchday = isGroup ? (groupMatchdays.TryGetValue(m.Id, out var md) ? md : null) : null;

            var (homeTeam, awayTeam, homeLabel, awayLabel) = ResolveTeams(m, isGroup, espnBracketData, knockoutMatchNumbers);

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

    private static (string Home, string Away, string? HomeLabel, string? AwayLabel) ResolveTeams(
        FdMatch m, bool isGroup,
        Dictionary<int, EspnBracketService.EspnBracketData> espnBracketData,
        Dictionary<int, int> knockoutMatchNumbers)
    {
        if (isGroup)
        {
            return (
                TranslateTeam(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName),
                TranslateTeam(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName),
                TranslateLabel(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName),
                TranslateLabel(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName));
        }

        if (espnBracketData.TryGetValue(m.Id, out var espn))
        {
            return (
                espn.HomeTeam != "TBD" ? espn.HomeTeam : TranslateTeam(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName),
                espn.AwayTeam != "TBD" ? espn.AwayTeam : TranslateTeam(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName),
                espn.HomeLabel,
                espn.AwayLabel);
        }

        var matchNum = knockoutMatchNumbers.TryGetValue(m.Id, out var mn) ? mn : 0;
        var (homeLabel, awayLabel) = Wc2026Bracket.GetSlotLabels(matchNum);
        return (
            TranslateTeam(m.HomeTeam?.Name ?? m.HomeTeam?.ShortName),
            TranslateTeam(m.AwayTeam?.Name ?? m.AwayTeam?.ShortName),
            homeLabel,
            awayLabel);
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
            return $"1st Group {name[winner.Length..].Trim()}";

        const string runnerUp = "Runner-up Group ";
        if (name.StartsWith(runnerUp, StringComparison.OrdinalIgnoreCase))
            return $"2nd Group {name[runnerUp.Length..].Trim()}";

        const string winnerMatch = "Winner Match ";
        if (name.StartsWith(winnerMatch, StringComparison.OrdinalIgnoreCase))
            return $"W. Match {name[winnerMatch.Length..].Trim()}";

        const string loserMatch = "Loser Match ";
        if (name.StartsWith(loserMatch, StringComparison.OrdinalIgnoreCase))
            return $"L. Match {name[loserMatch.Length..].Trim()}";

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
