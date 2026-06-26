using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Regex = System.Text.RegularExpressions.Regex;

namespace Prodea.Api.Services;

public class FixtureService(
    ProdeaDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<FixtureService> logger)
{
    public async Task<int> UpdateKnockoutTeamNamesAsync()
    {
        var tbdMatches = await db.Matches
            .Where(m => m.Phase != MatchPhase.Group && m.Status == MatchStatus.Scheduled
                        && (m.HomeTeam == "TBD" || m.AwayTeam == "TBD"))
            .ToListAsync();

        if (tbdMatches.Count == 0) return 0;

        // ESPN resuelve 1°/2° de grupo apenas termina el grupo, mucho más rápido que
        // football-data.org (que a veces demora horas en publicar el cruce). Para los 3°
        // puestos que avanzan (combinatoria entre grupos) seguimos dependiendo de
        // football-data.org, que es quien tiene la tabla oficial de cruces de FIFA.
        int updatedFromEspn = await TryResolveFromEspnStandingsAsync(tbdMatches);

        if (string.IsNullOrEmpty(config["FootballData:ApiKey"])) return updatedFromEspn;

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

            return updatedFromEspn + updated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al sincronizar equipos knockout");
            return updatedFromEspn;
        }
    }

    // Slot label generado por Wc2026Bracket: "1º Grupo A", "2º Grupo J", etc.
    // Los puestos 3º (combinatoria entre varios grupos) quedan afuera a propósito: ESPN no
    // resuelve esa combinatoria, así que esos labels se dejan para el fallback de football-data.org.
    private static readonly Regex SlotLabelRegex = new(@"^([12])º Grupo ([A-L])$");

    private async Task<int> TryResolveFromEspnStandingsAsync(List<Match> tbdMatches)
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

    private record EspnStandingsResponse(List<EspnGroupNode>? Children);
    private record EspnGroupNode(string? Name, EspnStandingsGroup? Standings);
    private record EspnStandingsGroup(List<EspnStandingsEntry>? Entries);
    private record EspnStandingsEntry(EspnTeamRef? Team, EspnNote? Note);
    private record EspnTeamRef(string? DisplayName);
    private record EspnNote(int? Rank);

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
            // Upsert: actualiza partidos existentes preservando predicciones, agrega los nuevos
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
            // DB vacía o solo seed: reemplaza todo (sin predicciones que preservar)
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

        // R32: bracket desde ESPN (más confiable que football-data.org para los cruces)
        var r32ApiMatches = knockoutApiMatches
            .Where(m => MapKnockoutPhase(m.Stage) == MatchPhase.R32)
            .ToList();
        var espnBracket = await FetchEspnR32BracketAsync(r32ApiMatches);

        // Match numbers: R32 secuencial por fecha (73-88), otras fases vía bracket estático
        var knockoutMatchNumbers = new Dictionary<int, int>();
        int r32Num = 73;
        foreach (var m in r32ApiMatches.OrderBy(m => m.UtcDate))
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
            else if (phase == MatchPhase.R32 && espnBracket.TryGetValue(m.Id, out var espn))
            {
                var fdHome = m.HomeTeam?.Name ?? m.HomeTeam?.ShortName;
                var fdAway = m.AwayTeam?.Name ?? m.AwayTeam?.ShortName;
                homeTeam = fdHome != null ? TranslateTeam(fdHome) : espn.HomeTeam;
                awayTeam = fdAway != null ? TranslateTeam(fdAway) : espn.AwayTeam;
                homeLabel = espn.HomeLabel;
                awayLabel = espn.AwayLabel;
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
        if (TeamNames.ContainsKey(name)) return null;

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
        return TeamNames.TryGetValue(name, out var t) ? t : name;
    }

    private static readonly Dictionary<string, string> TeamNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // CONMEBOL
        ["Argentina"]                    = "Argentina",
        ["Brazil"]                       = "Brasil",
        ["Colombia"]                     = "Colombia",
        ["Ecuador"]                      = "Ecuador",
        ["Paraguay"]                     = "Paraguay",
        ["Peru"]                         = "Perú",
        ["Uruguay"]                      = "Uruguay",
        ["Venezuela"]                    = "Venezuela",
        // UEFA
        ["Germany"]                      = "Alemania",
        ["Austria"]                      = "Austria",
        ["Belgium"]                      = "Bélgica",
        ["Bosnia and Herzegovina"]       = "Bosnia y Herzegovina",
        ["Bosnia-Herzegovina"]           = "Bosnia y Herzegovina",
        ["Croatia"]                      = "Croacia",
        ["Denmark"]                      = "Dinamarca",
        ["Spain"]                        = "España",
        ["Scotland"]                     = "Escocia",
        ["France"]                       = "Francia",
        ["Wales"]                        = "Gales",
        ["England"]                      = "Inglaterra",
        ["Italy"]                        = "Italia",
        ["Netherlands"]                  = "Países Bajos",
        ["Poland"]                       = "Polonia",
        ["Norway"]                       = "Noruega",
        ["Portugal"]                     = "Portugal",
        ["Czech Republic"]               = "República Checa",
        ["Czechia"]                      = "República Checa",
        ["Romania"]                      = "Rumania",
        ["Serbia"]                       = "Serbia",
        ["Sweden"]                       = "Suecia",
        ["Switzerland"]                  = "Suiza",
        ["Turkey"]                       = "Turquía",
        ["Türkiye"]                      = "Turquía",
        // CAF
        ["Algeria"]                      = "Argelia",
        ["Cape Verde"]                   = "Cabo Verde",
        ["Cape Verde Islands"]           = "Cabo Verde",
        ["Cameroon"]                     = "Camerún",
        ["Ivory Coast"]                  = "Costa de Marfil",
        ["Côte d'Ivoire"]                = "Costa de Marfil",
        ["Egypt"]                        = "Egipto",
        ["Ghana"]                        = "Ghana",
        ["Morocco"]                      = "Marruecos",
        ["Nigeria"]                      = "Nigeria",
        ["DR Congo"]                     = "R. D. del Congo",
        ["Congo DR"]                     = "R. D. del Congo",
        ["Democratic Republic of Congo"] = "R. D. del Congo",
        ["Senegal"]                      = "Senegal",
        ["South Africa"]                 = "Sudáfrica",
        ["Tunisia"]                      = "Túnez",
        // AFC
        ["Saudi Arabia"]                 = "Arabia Saudita",
        ["Australia"]                    = "Australia",
        ["Qatar"]                        = "Catar",
        ["South Korea"]                  = "Corea del Sur",
        ["Korea Republic"]               = "Corea del Sur",
        ["Iraq"]                         = "Irak",
        ["Iran"]                         = "Irán",
        ["IR Iran"]                      = "Irán",
        ["Japan"]                        = "Japón",
        ["Jordan"]                       = "Jordania",
        ["Uzbekistan"]                   = "Uzbekistán",
        // CONCACAF
        ["Canada"]                       = "Canadá",
        ["Costa Rica"]                   = "Costa Rica",
        ["Curaçao"]                      = "Curazao",
        ["Curacao"]                      = "Curazao",
        ["El Salvador"]                  = "El Salvador",
        ["United States"]                = "Estados Unidos",
        ["USA"]                          = "Estados Unidos",
        ["Haiti"]                        = "Haití",
        ["Honduras"]                     = "Honduras",
        ["Jamaica"]                      = "Jamaica",
        ["Mexico"]                       = "México",
        ["Panama"]                       = "Panamá",
        // OFC
        ["New Zealand"]                  = "Nueva Zelanda",
    };

    private async Task<Dictionary<int, EspnR32Data>> FetchEspnR32BracketAsync(List<FdMatch> r32Matches)
    {
        var result = new Dictionary<int, EspnR32Data>();
        if (r32Matches.Count == 0) return result;

        var byKickoff = new Dictionary<string, int>();
        foreach (var m in r32Matches)
        {
            var key = m.UtcDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm");
            byKickoff.TryAdd(key, m.Id);
        }

        var dates = r32Matches
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

                    result[externalId] = new EspnR32Data(homeTeam, awayTeam, homeLabel, awayLabel);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al obtener bracket ESPN para fecha {Date}", date);
            }
        }

        logger.LogInformation("R32 bracket desde ESPN: {Count}/{Total} partidos mapeados",
            result.Count, r32Matches.Count);
        return result;
    }

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

        var translated = EspnTeamMapping.Map(displayName);
        return (translated, null);
    }

    private record EspnR32Data(string HomeTeam, string AwayTeam, string? HomeLabel, string? AwayLabel);
    private record EspnScoreboardResponse(List<EspnScoreboardEvent>? Events);
    private record EspnScoreboardEvent(string? Date, List<EspnScoreboardCompetition>? Competitions);
    private record EspnScoreboardCompetition(List<EspnScoreboardCompetitor>? Competitors);
    private record EspnScoreboardCompetitor(string? HomeAway, EspnScoreboardTeam? Team);
    private record EspnScoreboardTeam(string? DisplayName);

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
