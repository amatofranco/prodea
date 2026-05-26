using Prodea.Api.Models;

namespace Prodea.Api.Data;

public static class WorldCup2026Seed
{
    public static List<Match> GetGroupStageMatches()
    {
        var matches = new List<Match>();
        int id = 1;

        // Group A — Mexico City / Guadalajara
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "México", AwayTeam = "Ecuador", MatchDate = new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Polonia", AwayTeam = "Arabia Saudita", MatchDate = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "México", AwayTeam = "Polonia", MatchDate = new DateTime(2026, 6, 16, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Arabia Saudita", AwayTeam = "Ecuador", MatchDate = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Polonia", AwayTeam = "Ecuador", MatchDate = new DateTime(2026, 6, 21, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Arabia Saudita", AwayTeam = "México", MatchDate = new DateTime(2026, 6, 21, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group B — Los Angeles
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Estados Unidos", AwayTeam = "Gales", MatchDate = new DateTime(2026, 6, 12, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Inglaterra", AwayTeam = "Irán", MatchDate = new DateTime(2026, 6, 13, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Estados Unidos", AwayTeam = "Inglaterra", MatchDate = new DateTime(2026, 6, 18, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Irán", AwayTeam = "Gales", MatchDate = new DateTime(2026, 6, 19, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Gales", AwayTeam = "Inglaterra", MatchDate = new DateTime(2026, 6, 23, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Irán", AwayTeam = "Estados Unidos", MatchDate = new DateTime(2026, 6, 23, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group C — Dallas / Houston
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Argentina", AwayTeam = "Honduras", MatchDate = new DateTime(2026, 6, 13, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Croacia", AwayTeam = "Marruecos", MatchDate = new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Argentina", AwayTeam = "Croacia", MatchDate = new DateTime(2026, 6, 19, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Marruecos", AwayTeam = "Honduras", MatchDate = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Croacia", AwayTeam = "Honduras", MatchDate = new DateTime(2026, 6, 24, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Marruecos", AwayTeam = "Argentina", MatchDate = new DateTime(2026, 6, 24, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group D — New York / Philadelphia
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Francia", AwayTeam = "Australia", MatchDate = new DateTime(2026, 6, 14, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Dinamarca", AwayTeam = "Túnez", MatchDate = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Francia", AwayTeam = "Dinamarca", MatchDate = new DateTime(2026, 6, 20, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Túnez", AwayTeam = "Australia", MatchDate = new DateTime(2026, 6, 21, 2, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Dinamarca", AwayTeam = "Australia", MatchDate = new DateTime(2026, 6, 25, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Túnez", AwayTeam = "Francia", MatchDate = new DateTime(2026, 6, 25, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group E — San Francisco / Seattle
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "España", AwayTeam = "Costa Rica", MatchDate = new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Alemania", AwayTeam = "Japón", MatchDate = new DateTime(2026, 6, 15, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "España", AwayTeam = "Alemania", MatchDate = new DateTime(2026, 6, 21, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Japón", AwayTeam = "Costa Rica", MatchDate = new DateTime(2026, 6, 22, 4, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Japón", AwayTeam = "España", MatchDate = new DateTime(2026, 6, 26, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Costa Rica", AwayTeam = "Alemania", MatchDate = new DateTime(2026, 6, 26, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group F — Toronto / Boston
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Bélgica", AwayTeam = "Canadá", MatchDate = new DateTime(2026, 6, 16, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Brasil", AwayTeam = "Senegal", MatchDate = new DateTime(2026, 6, 16, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Brasil", AwayTeam = "Bélgica", MatchDate = new DateTime(2026, 6, 22, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Senegal", AwayTeam = "Canadá", MatchDate = new DateTime(2026, 6, 22, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Bélgica", AwayTeam = "Senegal", MatchDate = new DateTime(2026, 6, 27, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Canadá", AwayTeam = "Brasil", MatchDate = new DateTime(2026, 6, 27, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Knockout placeholders (TBD teams)
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 1, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 2, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 3, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 4, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 5, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 6, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 7, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 8, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 4 },
            // QF
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 10, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 5 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 11, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 5 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 12, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 5 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 13, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 5 },
            // SF
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 15, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.SF, Matchday = 6 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 16, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.SF, Matchday = 6 },
            // Final
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 19, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Final, Matchday = 7 },
        ]);

        return matches;
    }
}
