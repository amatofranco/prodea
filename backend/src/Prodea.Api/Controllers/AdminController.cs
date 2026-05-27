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

    private static string TranslateTeam(string? name)
    {
        if (name == null) return "TBD";
        return TeamNames.TryGetValue(name, out var translated) ? translated : name;
    }

    private static readonly Dictionary<string, string> TeamNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // América del Norte
        ["Mexico"]                  = "México",
        ["United States"]           = "Estados Unidos",
        ["USA"]                     = "Estados Unidos",
        ["Canada"]                  = "Canadá",
        // América del Sur
        ["Brazil"]                  = "Brasil",
        ["Argentina"]               = "Argentina",
        ["Ecuador"]                 = "Ecuador",
        ["Uruguay"]                 = "Uruguay",
        ["Colombia"]                = "Colombia",
        ["Paraguay"]                = "Paraguay",
        ["Venezuela"]               = "Venezuela",
        ["Chile"]                   = "Chile",
        ["Bolivia"]                 = "Bolivia",
        ["Peru"]                    = "Perú",
        // América Central y Caribe
        ["Honduras"]                = "Honduras",
        ["Costa Rica"]              = "Costa Rica",
        ["El Salvador"]             = "El Salvador",
        ["Jamaica"]                 = "Jamaica",
        ["Panama"]                  = "Panamá",
        ["Trinidad and Tobago"]     = "Trinidad y Tobago",
        ["Cuba"]                    = "Cuba",
        ["Guatemala"]               = "Guatemala",
        ["Haiti"]                   = "Haití",
        // Europa
        ["Germany"]                 = "Alemania",
        ["France"]                  = "Francia",
        ["Spain"]                   = "España",
        ["England"]                 = "Inglaterra",
        ["Portugal"]                = "Portugal",
        ["Netherlands"]             = "Países Bajos",
        ["Italy"]                   = "Italia",
        ["Belgium"]                 = "Bélgica",
        ["Croatia"]                 = "Croacia",
        ["Denmark"]                 = "Dinamarca",
        ["Switzerland"]             = "Suiza",
        ["Austria"]                 = "Austria",
        ["Scotland"]                = "Escocia",
        ["Turkey"]                  = "Turquía",
        ["Türkiye"]                 = "Turquía",
        ["Poland"]                  = "Polonia",
        ["Serbia"]                  = "Serbia",
        ["Czech Republic"]          = "República Checa",
        ["Czechia"]                 = "República Checa",
        ["Romania"]                 = "Rumania",
        ["Hungary"]                 = "Hungría",
        ["Slovakia"]                = "Eslovaquia",
        ["Wales"]                   = "Gales",
        ["Ukraine"]                 = "Ucrania",
        ["Greece"]                  = "Grecia",
        ["Norway"]                  = "Noruega",
        ["Albania"]                 = "Albania",
        ["Slovenia"]                = "Eslovenia",
        ["Iceland"]                 = "Islandia",
        ["Sweden"]                  = "Suecia",
        ["Finland"]                 = "Finlandia",
        ["Russia"]                  = "Rusia",
        ["Georgia"]                 = "Georgia",
        ["Bosnia and Herzegovina"]  = "Bosnia y Herzegovina",
        ["North Macedonia"]         = "Macedonia del Norte",
        ["Kosovo"]                  = "Kosovo",
        ["Bulgaria"]                = "Bulgaria",
        ["Montenegro"]              = "Montenegro",
        ["Republic of Ireland"]     = "Irlanda",
        ["Ireland"]                 = "Irlanda",
        ["Northern Ireland"]        = "Irlanda del Norte",
        ["Luxembourg"]              = "Luxemburgo",
        // África
        ["Morocco"]                 = "Marruecos",
        ["Senegal"]                 = "Senegal",
        ["Nigeria"]                 = "Nigeria",
        ["Egypt"]                   = "Egipto",
        ["Cameroon"]                = "Camerún",
        ["Ghana"]                   = "Ghana",
        ["Ivory Coast"]             = "Costa de Marfil",
        ["Côte d'Ivoire"]           = "Costa de Marfil",
        ["Algeria"]                 = "Argelia",
        ["Tunisia"]                 = "Túnez",
        ["South Africa"]            = "Sudáfrica",
        ["Mali"]                    = "Mali",
        ["DR Congo"]                = "República D. del Congo",
        ["Democratic Republic of Congo"] = "República D. del Congo",
        ["Zambia"]                  = "Zambia",
        ["Tanzania"]                = "Tanzania",
        ["Kenya"]                   = "Kenia",
        ["Cape Verde"]              = "Cabo Verde",
        ["Gabon"]                   = "Gabón",
        ["Guinea"]                  = "Guinea",
        ["Zimbabwe"]                = "Zimbabue",
        ["Uganda"]                  = "Uganda",
        ["Mozambique"]              = "Mozambique",
        ["Burkina Faso"]            = "Burkina Faso",
        // Asia
        ["Japan"]                   = "Japón",
        ["South Korea"]             = "Corea del Sur",
        ["Korea Republic"]          = "Corea del Sur",
        ["Iran"]                    = "Irán",
        ["IR Iran"]                 = "Irán",
        ["Saudi Arabia"]            = "Arabia Saudita",
        ["Australia"]               = "Australia",
        ["Uzbekistan"]              = "Uzbekistán",
        ["Qatar"]                   = "Catar",
        ["Jordan"]                  = "Jordania",
        ["Iraq"]                    = "Irak",
        ["Oman"]                    = "Omán",
        ["Bahrain"]                 = "Bahrein",
        ["United Arab Emirates"]    = "Emiratos Árabes Unidos",
        ["China PR"]                = "China",
        ["China"]                   = "China",
        ["India"]                   = "India",
        ["Thailand"]                = "Tailandia",
        ["Vietnam"]                 = "Vietnam",
        ["Indonesia"]               = "Indonesia",
        ["Syria"]                   = "Siria",
        ["Kuwait"]                  = "Kuwait",
        ["Palestine"]               = "Palestina",
        ["Kyrgyzstan"]              = "Kirguistán",
        ["Tajikistan"]              = "Tayikistán",
        // Oceanía
        ["New Zealand"]             = "Nueva Zelanda",
        ["Fiji"]                    = "Fiyi",
        ["Papua New Guinea"]        = "Papúa Nueva Guinea",
        ["Solomon Islands"]         = "Islas Salomón",
        ["Tahiti"]                  = "Tahití",
        ["Vanuatu"]                 = "Vanuatu",
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
