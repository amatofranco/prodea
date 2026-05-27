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
    ILogger<FixtureService> logger)
{
    public async Task<(int count, string source)> ImportAsync(bool force = false)
    {
        if (await db.Matches.AnyAsync())
        {
            if (!force) return (0, "ya cargado");
            var existing = await db.Matches.ToListAsync();
            db.Matches.RemoveRange(existing);
            await db.SaveChangesAsync();
            logger.LogInformation("Fixture eliminado para reimportar ({Count} partidos)", existing.Count);
        }

        List<Match> matches;
        string source;

        var apiKey = config["FootballData:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                matches = await FetchFromApiAsync();
                source = "football-data.org";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo obtener el fixture de football-data.org, usando seed local");
                matches = WorldCup2026Seed.GetGroupStageMatches();
                source = "seed local (fallback)";
            }
        }
        else
        {
            logger.LogWarning("FootballData:ApiKey no configurado, usando seed local");
            matches = WorldCup2026Seed.GetGroupStageMatches();
            source = "seed local (sin API key)";
        }

        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();
        return (matches.Count, source);
    }

    private async Task<List<Match>> FetchFromApiAsync()
    {
        var client = httpClientFactory.CreateClient("FootballData");
        var response = await client.GetAsync("/v4/competitions/WC/matches");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FdMatchesResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Respuesta vacía de football-data.org");

        // Log stage names para debugging
        var stages = result.Matches.Select(m => $"{m.Stage}(group={m.Group})").Distinct();
        logger.LogInformation("Stages en respuesta de football-data.org: {Stages}", string.Join(", ", stages));

        var allMatches = result.Matches.OrderBy(m => m.UtcDate).ToList();

        // Identifica partidos de fase de grupos: stage GROUP_STAGE o tiene campo group
        var groupApiMatches = allMatches
            .Where(m => m.Stage == "GROUP_STAGE" || m.Group != null)
            .ToList();
        var knockoutApiMatches = allMatches.Except(groupApiMatches).ToList();

        // Calcula matchday por posición dentro de cada grupo (si hay campo group)
        // Fallback: divide los 72 partidos de grupos en tercios por fecha (24 cada uno)
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
            // Fallback: divide todos los partidos de grupos en 3 rondas iguales por fecha
            var sorted = groupApiMatches.OrderBy(m => m.UtcDate).ToList();
            int perRound = Math.Max(1, sorted.Count / 3);
            groupMatchdays = sorted
                .Select((m, i) => (m.Id, Matchday: Math.Min(3, i / perRound + 1)))
                .ToDictionary(x => x.Id, x => x.Matchday);
        }

        var matches = new List<Match>();
        int localId = 1;

        foreach (var m in allMatches)
        {
            bool isGroup = groupApiMatches.Contains(m);
            var phase = isGroup ? MatchPhase.Group : MapKnockoutPhase(m.Stage);
            int? matchday = isGroup
                ? (groupMatchdays.TryGetValue(m.Id, out var md) ? md : null)
                : null; // knockout matchday no se usa en frontend; se agrupa por phase

            matches.Add(new Match
            {
                Id = localId++,
                ExternalId = m.Id,
                HomeTeam = TranslateTeam(m.HomeTeam?.Name),
                AwayTeam = TranslateTeam(m.AwayTeam?.Name),
                HomeTeamLabel = TranslateLabel(m.HomeTeam?.Name),
                AwayTeamLabel = TranslateLabel(m.AwayTeam?.Name),
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

    private static MatchPhase MapKnockoutPhase(string? stage) => stage switch
    {
        "ROUND_OF_32"    => MatchPhase.R32,
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
            return $"1° Grupo {name[winner.Length..].Trim()}";

        const string runnerUp = "Runner-up Group ";
        if (name.StartsWith(runnerUp, StringComparison.OrdinalIgnoreCase))
            return $"2° Grupo {name[runnerUp.Length..].Trim()}";

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
    private record FdTeam(string? Name);
    private record FdScore([property: JsonPropertyName("fullTime")] FdFullTime? FullTime);
    private record FdFullTime(int? Home, int? Away);
}
