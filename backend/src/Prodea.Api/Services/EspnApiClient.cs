using System.Text.Json;
using System.Text.Json.Serialization;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class EspnApiClient(IHttpClientFactory httpClientFactory, ILogger<EspnApiClient> logger)
{
    private const string ScoreboardPath = "/apis/site/v2/sports/soccer/fifa.world/scoreboard";
    private const string SummaryPath    = "/apis/site/v2/sports/soccer/fifa.world/summary";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<EspnEvent>> FetchScoreboardAsync(DateTime utcDate, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var dateStr = utcDate.ToString("yyyyMMdd");
            var response = await client.GetAsync(
                $"{ScoreboardPath}?dates={dateStr}", ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ESPN API returned {Status} for date {Date}", response.StatusCode, dateStr);
                return [];
            }
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<EspnScoreboardResponse>(json, JsonOptions);
            return result?.Events ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching ESPN scoreboard");
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
            if (dateDiff >= 4) return false;

            var sameOrder = string.Equals(espnHome, match.HomeTeam, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(espnAway, match.AwayTeam, StringComparison.OrdinalIgnoreCase);
            var reversedOrder = string.Equals(espnHome, match.AwayTeam, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(espnAway, match.HomeTeam, StringComparison.OrdinalIgnoreCase);
            return sameOrder || reversedOrder;
        });
    }

    // El "home" que asigna ESPN para un cruce de knockout no siempre coincide con el que
    // tenemos cargado (ver Espana-Portugal Octavos 2026), por eso FindMatch acepta ambos
    // ordenes. Acá hay que detectar cuál es para no invertir el marcador al extraerlo.
    public static (int Home, int Away, string? Winner, int? HomePen, int? AwayPen) ExtractScore(EspnEvent espnEvent, Match match)
    {
        var comp = espnEvent.Competitions.FirstOrDefault();
        var homeComp = comp?.Competitors.FirstOrDefault(c => c.HomeAway == "home");
        var awayComp = comp?.Competitors.FirstOrDefault(c => c.HomeAway == "away");

        var swapped = string.Equals(MapTeam(homeComp?.Team.DisplayName ?? ""), match.AwayTeam, StringComparison.OrdinalIgnoreCase);

        int.TryParse(homeComp?.Score, out var homeCompScore);
        int.TryParse(awayComp?.Score, out var awayCompScore);
        var (home, away) = swapped ? (awayCompScore, homeCompScore) : (homeCompScore, awayCompScore);

        var homeCompWon = homeComp?.Winner == true;
        var awayCompWon = awayComp?.Winner == true;
        if (swapped) (homeCompWon, awayCompWon) = (awayCompWon, homeCompWon);

        string? winner = null;
        if (homeCompWon) winner = match.HomeTeam;
        else if (awayCompWon) winner = match.AwayTeam;

        var homePen = swapped ? awayComp?.ShootoutScore : homeComp?.ShootoutScore;
        var awayPen = swapped ? homeComp?.ShootoutScore : awayComp?.ShootoutScore;

        return (home, away, winner, homePen, awayPen);
    }

    public async Task<List<GoalInfo>> FetchGoalsAsync(string eventId, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Espn");
            var response = await client.GetAsync(
                $"{SummaryPath}?event={eventId}", ct);
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
            logger.LogWarning(ex, "Error fetching ESPN goals for event {EventId}", eventId);
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
        [property: JsonPropertyName("team")] EspnTeam Team,
        [property: JsonPropertyName("shootoutScore")] int? ShootoutScore
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
