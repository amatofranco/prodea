using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Regex = System.Text.RegularExpressions.Regex;

namespace Prodea.Api.Services;

public class EspnBracketService(
    ProdeaDbContext db,
    IHttpClientFactory httpClientFactory,
    EspnApiClient espn,
    ILogger<EspnBracketService> logger)
{
    private const string StandingsPath = "/apis/v2/sports/soccer/fifa.world/standings";

    public async Task<int> TryResolveFromEspnStandingsAsync(List<Match> tbdMatches)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var response = await client.GetAsync(StandingsPath);
            if (!response.IsSuccessStatusCode) return 0;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EspnStandingsResponse>(json, JsonOptions);
            if (result?.Children == null) return 0;

            var groupMatches = await db.Matches
                .Where(m => m.Phase == MatchPhase.Group && m.Group != null)
                .Select(m => new { m.Group, m.Status })
                .ToListAsync();
            var finishedLetters = groupMatches
                .GroupBy(m => m.Group!)
                .Where(g => g.All(m => m.Status == MatchStatus.Finished))
                .Select(g => g.Key.Replace("GROUP_", "", StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rankByGroup = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in result.Children)
            {
                if (g.Name == null || !g.Name.StartsWith("Group ", StringComparison.OrdinalIgnoreCase)) continue;
                var letter = g.Name["Group ".Length..].Trim();
                if (!finishedLetters.Contains(letter)) continue;

                var entries = g.Standings?.Entries;
                if (entries == null) continue;

                var ranks = new Dictionary<int, string>();
                foreach (var e in entries)
                {
                    if (e.Note?.Rank is int rank && e.Team?.DisplayName is string teamName)
                        ranks[rank] = EspnTeamMapping.Map(teamName);
                }
                rankByGroup[letter] = ranks;
            }

            int updated = 0;
            foreach (var match in tbdMatches)
            {
                if (match.HomeTeam == "TBD" && TryResolveSlot(match.HomeTeamLabel, rankByGroup, out var homeTeam))
                {
                    match.HomeTeam = homeTeam;
                    match.HomeTeamLabel = null;
                    updated++;
                }
                if (match.AwayTeam == "TBD" && TryResolveSlot(match.AwayTeamLabel, rankByGroup, out var awayTeam))
                {
                    match.AwayTeam = awayTeam;
                    match.AwayTeamLabel = null;
                    updated++;
                }
            }

            if (updated > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Knockout teams updated via ESPN standings: {Count}", updated);
            }

            return updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error syncing standings via ESPN");
            return 0;
        }
    }

    public async Task<int> TryResolveFromEspnScoreboardAsync(List<Match> tbdMatches)
    {
        var stillTbd = tbdMatches
            .Where(m => m.ExternalId.HasValue && (m.HomeTeam == "TBD" || m.AwayTeam == "TBD"))
            .ToList();
        if (stillTbd.Count == 0) return 0;

        try
        {
            var byKickoff = new Dictionary<string, Match>();
            foreach (var m in stillTbd)
                byKickoff.TryAdd(FormatKickoffKey(m.MatchDate), m);

            var eventsByKickoff = await FetchEventsByKickoffAsync(stillTbd.Select(m => m.MatchDate));

            int updated = 0;
            foreach (var (key, ev) in eventsByKickoff)
            {
                if (!byKickoff.TryGetValue(key, out var match)) continue;
                var comp = ev.Competitions.FirstOrDefault();
                if (comp == null) continue;

                var homeComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "home");
                var awayComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "away");

                if (match.HomeTeam == "TBD"
                    && homeComp != null
                    && EspnTeamMapping.EspnToSpanish.ContainsKey(homeComp.Team.DisplayName))
                {
                    match.HomeTeam = EspnTeamMapping.Map(homeComp.Team.DisplayName);
                    match.HomeTeamLabel = null;
                    updated++;
                }

                if (match.AwayTeam == "TBD"
                    && awayComp != null
                    && EspnTeamMapping.EspnToSpanish.ContainsKey(awayComp.Team.DisplayName))
                {
                    match.AwayTeam = EspnTeamMapping.Map(awayComp.Team.DisplayName);
                    match.AwayTeamLabel = null;
                    updated++;
                }
            }

            if (updated > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Knockout teams updated via ESPN scoreboard: {Count}", updated);
            }

            return updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error resolving teams via ESPN scoreboard");
            return 0;
        }
    }

    public async Task<Dictionary<int, EspnBracketData>> FetchKnockoutBracketAsync(
        List<(int ExternalId, DateTime UtcDate)> knockoutMatches)
    {
        var result = new Dictionary<int, EspnBracketData>();
        if (knockoutMatches.Count == 0) return result;

        var byKickoff = new Dictionary<string, int>();
        foreach (var m in knockoutMatches)
            byKickoff.TryAdd(FormatKickoffKey(m.UtcDate), m.ExternalId);

        try
        {
            var eventsByKickoff = await FetchEventsByKickoffAsync(
                knockoutMatches.Select(m => m.UtcDate));

            foreach (var (key, ev) in eventsByKickoff)
            {
                if (!byKickoff.TryGetValue(key, out var externalId)) continue;
                var comp = ev.Competitions.FirstOrDefault();
                if (comp == null) continue;

                var homeComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "home");
                var awayComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "away");

                var (homeTeam, homeLabel) = ParseEspnBracketTeam(homeComp?.Team.DisplayName);
                var (awayTeam, awayLabel) = ParseEspnBracketTeam(awayComp?.Team.DisplayName);

                result[externalId] = new EspnBracketData(homeTeam, awayTeam, homeLabel, awayLabel);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al obtener bracket ESPN");
        }

        logger.LogInformation("Knockout bracket desde ESPN: {Count}/{Total} partidos mapeados",
            result.Count, knockoutMatches.Count);
        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<Dictionary<string, EspnApiClient.EspnEvent>> FetchEventsByKickoffAsync(
        IEnumerable<DateTime> matchDates)
    {
        var dates = matchDates
            .Select(d => d.ToUniversalTime().Date)
            .Distinct()
            .ToList();

        var result = new Dictionary<string, EspnApiClient.EspnEvent>();
        foreach (var date in dates)
        {
            var events = await espn.FetchScoreboardAsync(date, CancellationToken.None);
            foreach (var ev in events)
                result.TryAdd(FormatKickoffKey(ev.Date), ev);
        }
        return result;
    }

    private static string FormatKickoffKey(DateTime dt) =>
        dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm");

    private static readonly Regex SlotLabelRegex = new(@"^([12])(?:st|nd) Group ([A-L])$");

    private static bool TryResolveSlot(string? label, Dictionary<string, Dictionary<int, string>> rankByGroup, out string team)
    {
        team = "";
        if (label == null) return false;

        var regexMatch = SlotLabelRegex.Match(label);
        if (!regexMatch.Success) return false;

        var rank = int.Parse(regexMatch.Groups[1].Value);
        var letter = regexMatch.Groups[2].Value;

        if (!rankByGroup.TryGetValue(letter, out var ranks) || !ranks.TryGetValue(rank, out var teamName))
            return false;

        team = teamName;
        return true;
    }

    private static readonly (string Prefix, string Suffix, MatchPhase Phase, string Tag)[] RoundPatterns =
    [
        ("Round of 32 ", " Winner", MatchPhase.R32, "W."),
        ("Round of 16 ", " Winner", MatchPhase.R16, "W."),
        ("Quarterfinal ", " Winner", MatchPhase.QF, "W."),
        ("Quarter-Final ", " Winner", MatchPhase.QF, "W."),
        ("Semifinal ", " Winner", MatchPhase.SF, "W."),
        ("Semi-Final ", " Winner", MatchPhase.SF, "W."),
        ("Semifinal ", " Loser", MatchPhase.SF, "L."),
        ("Semi-Final ", " Loser", MatchPhase.SF, "L."),
    ];

    private static (string Team, string? Label) ParseEspnBracketTeam(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return ("TBD", null);

        if (displayName.StartsWith("Group ", StringComparison.OrdinalIgnoreCase)
            && displayName.EndsWith(" Winner", StringComparison.OrdinalIgnoreCase))
        {
            var letter = displayName["Group ".Length..^" Winner".Length].Trim();
            return ("TBD", $"1st Group {letter}");
        }

        if (displayName.StartsWith("Group ", StringComparison.OrdinalIgnoreCase)
            && displayName.EndsWith(" 2nd Place", StringComparison.OrdinalIgnoreCase))
        {
            var letter = displayName["Group ".Length..^" 2nd Place".Length].Trim();
            return ("TBD", $"2nd Group {letter}");
        }

        const string thirdPlace = "Third Place Group ";
        if (displayName.StartsWith(thirdPlace, StringComparison.OrdinalIgnoreCase))
        {
            var groups = displayName[thirdPlace.Length..].Trim();
            return ("TBD", $"3rd Groups {groups}");
        }

        foreach (var (prefix, suffix, phase, tag) in RoundPatterns)
        {
            if (displayName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && displayName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var numStr = displayName[prefix.Length..^suffix.Length].Trim();
                if (int.TryParse(numStr, out var num))
                {
                    var matchNum = Wc2026Bracket.MatchNumberByDatePosition(phase, num - 1);
                    if (matchNum.HasValue)
                        return ("TBD", $"{tag} P{matchNum.Value}");
                }
                return ("TBD", null);
            }
        }

        var translated = EspnTeamMapping.Map(displayName);
        return (translated, null);
    }

    // ── DTOs ────────────────────────────────────────────────────────────

    public record EspnBracketData(string HomeTeam, string AwayTeam, string? HomeLabel, string? AwayLabel);

    private record EspnStandingsResponse(List<EspnGroupNode>? Children);
    private record EspnGroupNode(string? Name, EspnStandingsGroup? Standings);
    private record EspnStandingsGroup(List<EspnStandingsEntry>? Entries);
    private record EspnStandingsEntry(EspnTeamRef? Team, EspnNote? Note);
    private record EspnTeamRef(string? DisplayName);
    private record EspnNote(int? Rank);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}
