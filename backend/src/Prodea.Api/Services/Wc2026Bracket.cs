using Prodea.Api.Models;

namespace Prodea.Api.Services;

/// <summary>
/// Bracket fijo del Mundial 2026, derivado del sorteo de diciembre 2024.
/// Los partidos de cada fase se numeran según el orden por fecha de la API.
/// </summary>
public static class Wc2026Bracket
{
    private static readonly Dictionary<int, (string Home, string Away)> Slots = new()
    {
        // Dieciseisavos de Final (73–88)
        [73]  = ("2º Grupo A",  "2º Grupo B"),
        [74]  = ("1º Grupo C",  "3º Grupos A/B/C/D/F"),
        [75]  = ("1º Grupo F",  "2º Grupo C"),
        [76]  = ("1º Grupo E",  "2º Grupo F"),
        [77]  = ("1º Grupo I",  "3º Grupos C/D/F/G/H"),
        [78]  = ("2º Grupo E",  "2º Grupo I"),
        [79]  = ("1º Grupo A",  "3º Grupos C/E/F/H/I"),
        [80]  = ("1º Grupo L",  "3º Grupos E/H/I/J/K"),
        [81]  = ("1º Grupo D",  "3º Grupos B/E/F/I/J"),
        [82]  = ("1º Grupo G",  "3º Grupos A/E/H/I/J"),
        [83]  = ("2º Grupo K",  "2º Grupo L"),
        [84]  = ("1º Grupo H",  "2º Grupo J"),
        [85]  = ("1º Grupo B",  "3º Grupos E/F/G/I/J"),
        [86]  = ("1º Grupo J",  "2º Grupo H"),
        [87]  = ("1º Grupo K",  "3º Grupos D/E/I/J/L"),
        [88]  = ("2º Grupo D",  "2º Grupo G"),
        // Octavos de Final (89–96)
        [89]  = ("Gan. P74",   "Gan. P77"),
        [90]  = ("Gan. P73",   "Gan. P75"),
        [91]  = ("Gan. P76",   "Gan. P78"),
        [92]  = ("Gan. P79",   "Gan. P80"),
        [93]  = ("Gan. P83",   "Gan. P84"),
        [94]  = ("Gan. P81",   "Gan. P82"),
        [95]  = ("Gan. P86",   "Gan. P88"),
        [96]  = ("Gan. P85",   "Gan. P87"),
        // Cuartos de Final (97–100)
        [97]  = ("Gan. P89",   "Gan. P90"),
        [98]  = ("Gan. P91",   "Gan. P92"),
        [99]  = ("Gan. P93",   "Gan. P94"),
        [100] = ("Gan. P95",   "Gan. P96"),
        // Semifinales (101–102)
        [101] = ("Gan. P97",   "Gan. P98"),
        [102] = ("Gan. P99",   "Gan. P100"),
        // Tercer Puesto (103)
        [103] = ("Per. P101",  "Per. P102"),
        // Final (104)
        [104] = ("Gan. P101",  "Gan. P102"),
    };

    private static readonly Dictionary<MatchPhase, int> PhaseStart = new()
    {
        [MatchPhase.R32]        = 73,
        [MatchPhase.R16]        = 89,
        [MatchPhase.QF]         = 97,
        [MatchPhase.SF]         = 101,
        [MatchPhase.ThirdPlace] = 103,
        [MatchPhase.Final]      = 104,
    };

    /// <summary>
    /// Construye un mapa ExternalId → número de partido FIFA, ordenando por fecha dentro de cada fase.
    /// </summary>
    public static Dictionary<int, int> BuildMatchNumberMap(
        IEnumerable<(int ExternalId, MatchPhase Phase, DateTime UtcDate)> knockoutMatches)
    {
        return knockoutMatches
            .GroupBy(m => m.Phase)
            .SelectMany(g =>
            {
                var start = PhaseStart.GetValueOrDefault(g.Key, 0);
                return g.OrderBy(m => m.UtcDate)
                        .Select((m, i) => (m.ExternalId, MatchNumber: start + i));
            })
            .ToDictionary(x => x.ExternalId, x => x.MatchNumber);
    }

    public static (string? Home, string? Away) GetSlotLabels(int matchNumber) =>
        Slots.TryGetValue(matchNumber, out var slot) ? slot : (null, null);
}
