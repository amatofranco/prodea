using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Regex = System.Text.RegularExpressions.Regex;

namespace Prodea.Api.Services;

public class EspnBracketService(
    ProdeaDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<EspnBracketService> logger)
{
    public async Task<int> TryResolveFromEspnStandingsAsync(List<Match> tbdMatches)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var response = await client.GetAsync("/apis/v2/sports/soccer/fifa.world/standings");
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
                logger.LogInformation("Equipos knockout actualizados vía ESPN standings: {Count}", updated);
            }

            return updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al sincronizar standings vía ESPN");
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
            {
                var key = m.MatchDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm");
                byKickoff.TryAdd(key, m);
            }

            var dates = stillTbd
                .Select(m => m.MatchDate.ToUniversalTime().ToString("yyyyMMdd"))
                .Distinct()
                .ToList();

            var client = httpClientFactory.CreateClient("Espn");
            int updated = 0;

            foreach (var date in dates)
            {
                var response = await client.GetAsync(
                    $"/apis/site/v2/sports/soccer/fifa.world/scoreboard?dates={date}");
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var scoreboard = JsonSerializer.Deserialize<EspnScoreboardResponse>(json, JsonOptions);
                if (scoreboard?.Events == null) continue;

                foreach (var ev in scoreboard.Events)
                {
                    if (ev.Date == null || ev.Competitions is not { Count: > 0 }) continue;
                    if (!DateTime.TryParse(ev.Date, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var evDate))
                        continue;

                    var evKey = evDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm");
                    if (!byKickoff.TryGetValue(evKey, out var match)) continue;

                    var comp = ev.Competitions[0];
                    if (comp.Competitors is not { Count: >= 2 }) continue;

                    var homeComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "home");
                    var awayComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "away");

                    if (match.HomeTeam == "TBD"
                        && homeComp?.Team?.DisplayName != null
                        && EspnTeamMapping.EspnToSpanish.ContainsKey(homeComp.Team.DisplayName))
                    {
                        match.HomeTeam = EspnTeamMapping.Map(homeComp.Team.DisplayName);
                        match.HomeTeamLabel = null;
                        updated++;
                    }

                    if (match.AwayTeam == "TBD"
                        && awayComp?.Team?.DisplayName != null
                        && EspnTeamMapping.EspnToSpanish.ContainsKey(awayComp.Team.DisplayName))
                    {
                        match.AwayTeam = EspnTeamMapping.Map(awayComp.Team.DisplayName);
                        match.AwayTeamLabel = null;
                        updated++;
                    }
                }
            }

            if (updated > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Equipos knockout actualizados vía ESPN scoreboard: {Count}", updated);
            }

            return updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al resolver equipos vía ESPN scoreboard");
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
        {
            var key = m.UtcDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm");
            byKickoff.TryAdd(key, m.ExternalId);
        }

        var dates = knockoutMatches
            .Select(m => m.UtcDate.ToUniversalTime().ToString("yyyyMMdd"))
            .Distinct()
            .ToList();

        var client = httpClientFactory.CreateClient("Espn");

        foreach (var date in dates)
        {
            try
            {
                var response = await client.GetAsync(
                    $"/apis/site/v2/sports/soccer/fifa.world/scoreboard?dates={date}");
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var scoreboard = JsonSerializer.Deserialize<EspnScoreboardResponse>(json, JsonOptions);
                if (scoreboard?.Events == null) continue;

                foreach (var ev in scoreboard.Events)
                {
                    if (ev.Date == null || ev.Competitions is not { Count: > 0 }) continue;
                    if (!DateTime.TryParse(ev.Date, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var evDate))
                        continue;

                    var evKey = evDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm");
                    if (!byKickoff.TryGetValue(evKey, out var externalId)) continue;

                    var comp = ev.Competitions[0];
                    if (comp.Competitors is not { Count: >= 2 }) continue;

                    var homeComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "home");
                    var awayComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "away");

                    var (homeTeam, homeLabel) = ParseEspnBracketTeam(homeComp?.Team?.DisplayName);
                    var (awayTeam, awayLabel) = ParseEspnBracketTeam(awayComp?.Team?.DisplayName);

                    result[externalId] = new EspnBracketData(homeTeam, awayTeam, homeLabel, awayLabel);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al obtener bracket ESPN para fecha {Date}", date);
            }
        }

        logger.LogInformation("Knockout bracket desde ESPN: {Count}/{Total} partidos mapeados",
            result.Count, knockoutMatches.Count);
        return result;
    }

    // Slot label generado por Wc2026Bracket: "1º Grupo A", "2º Grupo J", etc.
    private static readonly Regex SlotLabelRegex = new(@"^([12])º Grupo ([A-L])$");

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
        ("Round of 32 ", " Winner", MatchPhase.R32, "Gan."),
        ("Round of 16 ", " Winner", MatchPhase.R16, "Gan."),
        ("Quarterfinal ", " Winner", MatchPhase.QF, "Gan."),
        ("Quarter-Final ", " Winner", MatchPhase.QF, "Gan."),
        ("Semifinal ", " Winner", MatchPhase.SF, "Gan."),
        ("Semi-Final ", " Winner", MatchPhase.SF, "Gan."),
        ("Semifinal ", " Loser", MatchPhase.SF, "Per."),
        ("Semi-Final ", " Loser", MatchPhase.SF, "Per."),
    ];

    private static (string Team, string? Label) ParseEspnBracketTeam(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return ("TBD", null);

        if (displayName.StartsWith("Group ", StringComparison.OrdinalIgnoreCase)
            && displayName.EndsWith(" Winner", StringComparison.OrdinalIgnoreCase))
        {
            var letter = displayName["Group ".Length..^" Winner".Length].Trim();
            return ("TBD", $"1º Grupo {letter}");
        }

        if (displayName.StartsWith("Group ", StringComparison.OrdinalIgnoreCase)
            && displayName.EndsWith(" 2nd Place", StringComparison.OrdinalIgnoreCase))
        {
            var letter = displayName["Group ".Length..^" 2nd Place".Length].Trim();
            return ("TBD", $"2º Grupo {letter}");
        }

        const string thirdPlace = "Third Place Group ";
        if (displayName.StartsWith(thirdPlace, StringComparison.OrdinalIgnoreCase))
        {
            var groups = displayName[thirdPlace.Length..].Trim();
            return ("TBD", $"3º Grupos {groups}");
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

    public record EspnBracketData(string HomeTeam, string AwayTeam, string? HomeLabel, string? AwayLabel);

    private record EspnStandingsResponse(List<EspnGroupNode>? Children);
    private record EspnGroupNode(string? Name, EspnStandingsGroup? Standings);
    private record EspnStandingsGroup(List<EspnStandingsEntry>? Entries);
    private record EspnStandingsEntry(EspnTeamRef? Team, EspnNote? Note);
    private record EspnTeamRef(string? DisplayName);
    private record EspnNote(int? Rank);

    private record EspnScoreboardResponse(List<EspnScoreboardEvent>? Events);
    private record EspnScoreboardEvent(string? Date, List<EspnScoreboardCompetition>? Competitions);
    private record EspnScoreboardCompetition(List<EspnScoreboardCompetitor>? Competitors);
    private record EspnScoreboardCompetitor(string? HomeAway, EspnScoreboardTeam? Team);
    private record EspnScoreboardTeam(string? DisplayName);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}
