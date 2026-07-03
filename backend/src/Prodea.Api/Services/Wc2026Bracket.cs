using Prodea.Api.Models;

namespace Prodea.Api.Services;

/// <summary>
/// Fixed World Cup 2026 bracket, derived from the December 2024 draw.
/// Match numbers follow chronological order from the API.
/// </summary>
public static class Wc2026Bracket
{
    private static readonly Dictionary<int, (string Home, string Away)> Slots = new()
    {
        // Round of 32 (73–88)
        [73]  = ("2nd Group A",  "2nd Group B"),
        [74]  = ("1st Group C",  "3rd Groups A/B/C/D/F"),
        [75]  = ("1st Group F",  "2nd Group C"),
        [76]  = ("1st Group E",  "2nd Group F"),
        [77]  = ("1st Group I",  "3rd Groups C/D/F/G/H"),
        [78]  = ("2nd Group E",  "2nd Group I"),
        [79]  = ("1st Group A",  "3rd Groups C/E/F/H/I"),
        [80]  = ("1st Group L",  "3rd Groups E/H/I/J/K"),
        [81]  = ("1st Group D",  "3rd Groups B/E/F/I/J"),
        [82]  = ("1st Group G",  "3rd Groups A/E/H/I/J"),
        [83]  = ("2nd Group K",  "2nd Group L"),
        [84]  = ("1st Group H",  "2nd Group J"),
        [85]  = ("1st Group B",  "3rd Groups E/F/G/I/J"),
        [86]  = ("1st Group J",  "2nd Group H"),
        [87]  = ("1st Group K",  "3rd Groups D/E/I/J/L"),
        [88]  = ("2nd Group D",  "2nd Group G"),
        // Round of 16 (89–96)
        [89]  = ("W. P74",   "W. P77"),
        [90]  = ("W. P73",   "W. P75"),
        [91]  = ("W. P76",   "W. P78"),
        [92]  = ("W. P79",   "W. P80"),
        [93]  = ("W. P83",   "W. P84"),
        [94]  = ("W. P81",   "W. P82"),
        [95]  = ("W. P86",   "W. P88"),
        [96]  = ("W. P85",   "W. P87"),
        // Quarter-finals (97–100)
        [97]  = ("W. P89",   "W. P90"),
        [98]  = ("W. P91",   "W. P92"),
        [99]  = ("W. P93",   "W. P94"),
        [100] = ("W. P95",   "W. P96"),
        // Semi-finals (101–102)
        [101] = ("W. P97",   "W. P98"),
        [102] = ("W. P99",   "W. P100"),
        // Third place (103)
        [103] = ("L. P101",  "L. P102"),
        // Final (104)
        [104] = ("W. P101",  "W. P102"),
    };

    // Orden cronológico real de los números de partido FIFA dentro de cada fase. No siempre
    // coincide con el orden numérico (ej. en Octavos el 76 se juega antes que el 74) —
    // por eso no se puede inferir el número de partido como "PhaseStart + posición por fecha".
    private static readonly Dictionary<MatchPhase, int[]> ChronologicalOrder = new()
    {
        [MatchPhase.R32]        = [73, 76, 74, 75, 78, 77, 79, 80, 82, 81, 84, 83, 85, 88, 86, 87],
        [MatchPhase.R16]        = [90, 89, 91, 92, 93, 94, 95, 96],
        [MatchPhase.QF]         = [97, 98, 99, 100],
        [MatchPhase.SF]         = [101, 102],
        [MatchPhase.ThirdPlace] = [103],
        [MatchPhase.Final]      = [104],
    };

    public static Dictionary<int, int> BuildMatchNumberMap(
        IEnumerable<(int ExternalId, MatchPhase Phase, DateTime UtcDate)> knockoutMatches)
    {
        var result = new Dictionary<int, int>();
        foreach (var phaseGroup in knockoutMatches.GroupBy(m => m.Phase))
        {
            var order = ChronologicalOrder.GetValueOrDefault(phaseGroup.Key, []);
            if (order.Length == 0) continue;

            var sorted = phaseGroup.OrderBy(m => m.UtcDate).ToArray();
            for (int i = 0; i < sorted.Length && i < order.Length; i++)
                result[sorted[i].ExternalId] = order[i];
        }
        return result;
    }

    public static (string? Home, string? Away) GetSlotLabels(int matchNumber) =>
        Slots.TryGetValue(matchNumber, out var slot) ? slot : (null, null);

    public static int? MatchNumberByDatePosition(MatchPhase phase, int zeroBasedPosition)
    {
        if (!ChronologicalOrder.TryGetValue(phase, out var order)) return null;
        return zeroBasedPosition >= 0 && zeroBasedPosition < order.Length ? order[zeroBasedPosition] : null;
    }
}
