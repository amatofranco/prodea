using System.Text.Json;
using System.Text.Json.Serialization;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class EspnApiClient(IHttpClientFactory httpClientFactory, ILogger<EspnApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<EspnEvent>> FetchScoreboardAsync(DateTime utcDate, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var dateStr = utcDate.ToString("yyyyMMdd");
            var response = await client.GetAsync(
                $"/apis/site/v2/sports/soccer/fifa.world/scoreboard?dates={dateStr}", ct);
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

    public static EspnEvent? FindMatch(IList<EspnEvent> events, Match match)
    {
        return events.FirstOrDefault(e =>
        {
            var comp = e.Competitions.FirstOrDefault();
            if (comp == null) return false;

            var homeComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "home");
            var awayComp = comp.Competitors.FirstOrDefault(c => c.HomeAway == "away");
            if (homeComp == null || awayComp == null) return false;

            var espnHome = MapTeam(homeComp.Team.DisplayName);
            var espnAway = MapTeam(awayComp.Team.DisplayName);

            var dateDiff = Math.Abs((e.Date - match.MatchDate).TotalHours);
            return dateDiff < 4 &&
                   string.Equals(espnHome, match.HomeTeam, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(espnAway, match.AwayTeam, StringComparison.OrdinalIgnoreCase);
        });
    }

    public static (int Home, int Away, string? Winner) ExtractScore(EspnEvent espnEvent, Match match)
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

    public async Task<List<GoalInfo>> FetchGoalsAsync(string eventId, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var response = await client.GetAsync(
                $"/apis/site/v2/sports/soccer/fifa.world/summary?event={eventId}", ct);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync(ct);
            var summary = JsonSerializer.Deserialize<EspnSummaryResponse>(json, JsonOptions);
            if (summary?.KeyEvents == null) return [];

            return summary.KeyEvents
                .Where(e => e.ScoringPlay && (
                    e.Type.TypeStr.StartsWith("goal", StringComparison.OrdinalIgnoreCase) ||
                    e.Type.TypeStr.StartsWith("penalty---scored", StringComparison.OrdinalIgnoreCase) ||
                    e.Type.TypeStr.Equals("own-goal", StringComparison.OrdinalIgnoreCase)))
                .Select(e =>
                {
                    var name = e.Participants?.FirstOrDefault()?.Athlete.DisplayName ?? "?";
                    var isPen = e.Type.TypeStr.StartsWith("penalty---scored", StringComparison.OrdinalIgnoreCase);
                    var isOwnGoal = e.Type.TypeStr.Equals("own-goal", StringComparison.OrdinalIgnoreCase);
                    var suffix = isPen ? " (pen.)" : isOwnGoal ? " (GEC)" : "";
                    return new GoalInfo(
                        Scorer: $"{name}{suffix}",
                        Team: MapTeam(e.Team?.DisplayName ?? ""),
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

    public static string MapTeam(string espnName) => EspnTeamMapping.Map(espnName);

    public static (int Base, int Extra) SplitClock(string displayClock)
    {
        var trimmed = displayClock.TrimEnd('\'');
        var sep = trimmed.IndexOf("'+", StringComparison.Ordinal);
        if (sep >= 0)
        {
            int.TryParse(trimmed[..sep], out var b);
            int.TryParse(trimmed[(sep + 2)..].TrimEnd('\''), out var e);
            return (b, e);
        }
        int.TryParse(trimmed.TrimEnd('\''), out var baseMin);
        return (baseMin, 0);
    }

    public static int? ParseMinute(string? displayClock, int period)
    {
        if (string.IsNullOrEmpty(displayClock)) return null;
        var (baseMin, extra) = SplitClock(displayClock);
        if (baseMin == 0) return null;
        return baseMin + extra;
    }

    public static string? FormatDisplayClock(string? displayClock)
    {
        if (string.IsNullOrEmpty(displayClock)) return null;
        var (baseMin, extra) = SplitClock(displayClock);
        if (baseMin == 0) return null;
        return $"{baseMin + extra}'";
    }

    // ── DTOs ────────────────────────────────────────────────────────────

    public record GoalInfo(string Scorer, string Team, string Minute);

    public record EspnScoreboardResponse(
        [property: JsonPropertyName("events")] List<EspnEvent> Events
    );

    public record EspnEvent(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("status")] EspnStatus Status,
        [property: JsonPropertyName("competitions")] List<EspnCompetition> Competitions
    );

    public record EspnStatus(
        [property: JsonPropertyName("clock")] double Clock,
        [property: JsonPropertyName("displayClock")] string DisplayClock,
        [property: JsonPropertyName("period")] int Period,
        [property: JsonPropertyName("type")] EspnStatusType Type
    );

    public record EspnStatusType(
        [property: JsonPropertyName("name")] string Name
    );

    public record EspnCompetition(
        [property: JsonPropertyName("competitors")] List<EspnCompetitor> Competitors
    );

    public record EspnCompetitor(
        [property: JsonPropertyName("homeAway")] string HomeAway,
        [property: JsonPropertyName("score")] string? Score,
        [property: JsonPropertyName("winner")] bool Winner,
        [property: JsonPropertyName("team")] EspnTeam Team
    );

    public record EspnTeam(
        [property: JsonPropertyName("displayName")] string DisplayName
    );

    public record EspnSummaryResponse(
        [property: JsonPropertyName("keyEvents")] List<EspnKeyEvent>? KeyEvents
    );

    public record EspnKeyEvent(
        [property: JsonPropertyName("scoringPlay")] bool ScoringPlay,
        [property: JsonPropertyName("type")] EspnKeyEventType Type,
        [property: JsonPropertyName("clock")] EspnKeyEventClock? Clock,
        [property: JsonPropertyName("team")] EspnKeyEventTeam? Team,
        [property: JsonPropertyName("participants")] List<EspnKeyEventParticipant>? Participants
    );

    public record EspnKeyEventType(
        [property: JsonPropertyName("type")] string TypeStr
    );

    public record EspnKeyEventClock(
        [property: JsonPropertyName("displayValue")] string? DisplayValue
    );

    public record EspnKeyEventTeam(
        [property: JsonPropertyName("displayName")] string DisplayName
    );

    public record EspnKeyEventParticipant(
        [property: JsonPropertyName("athlete")] EspnKeyEventAthlete Athlete
    );

    public record EspnKeyEventAthlete(
        [property: JsonPropertyName("displayName")] string DisplayName
    );
}
