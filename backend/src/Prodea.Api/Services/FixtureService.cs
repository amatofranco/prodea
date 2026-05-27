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

        // Calcula matchday de fase de grupos por posición dentro de cada grupo
        // (la API puede devolver matchday=1 para todos los partidos de grupos)
        var groupMatchdays = result.Matches
            .Where(m => MapPhase(m.Stage, m.Group) == MatchPhase.Group && m.Group != null)
            .GroupBy(m => m.Group)
            .SelectMany(g =>
            {
                var sorted = g.OrderBy(m => m.UtcDate).ToList();
                return sorted.Select((m, i) => (m.Id, Matchday: i / 2 + 1));
            })
            .ToDictionary(x => x.Id, x => x.Matchday);

        var matches = new List<Match>();
        int localId = 1;

        foreach (var m in result.Matches)
        {
            var phase = MapPhase(m.Stage, m.Group);
            int? matchday = phase == MatchPhase.Group
                ? (groupMatchdays.TryGetValue(m.Id, out var md) ? md : m.Matchday)
                : MapKnockoutMatchday(phase);

            matches.Add(new Match
            {
                Id = localId++,
                ExternalId = m.Id,
                HomeTeam = TranslateTeam(m.HomeTeam?.Name),
                AwayTeam = TranslateTeam(m.AwayTeam?.Name),
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

    private static MatchPhase MapPhase(string? stage, string? group = null) => stage switch
    {
        "GROUP_STAGE"    => MatchPhase.Group,
        "ROUND_OF_32"    => MatchPhase.R32,
        "ROUND_OF_16"    => MatchPhase.R16,
        "QUARTER_FINALS" => MatchPhase.QF,
        "SEMI_FINALS"    => MatchPhase.SF,
        "THIRD_PLACE"    => MatchPhase.ThirdPlace,
        "FINAL"          => MatchPhase.Final,
        // Si tiene grupo asignado es fase de grupos aunque el stage sea desconocido;
        // si no tiene grupo es fase eliminatoria
        _ => group != null ? MatchPhase.Group : MatchPhase.R32,
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
