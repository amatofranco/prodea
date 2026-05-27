using Prodea.Api.Models;

namespace Prodea.Api.Data;

public static class WorldCup2026Seed
{
    public static List<Match> GetGroupStageMatches()
    {
        var matches = new List<Match>();
        int id = 1;

        // Group A
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "México", AwayTeam = "Ecuador", MatchDate = new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Polonia", AwayTeam = "Arabia Saudita", MatchDate = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "México", AwayTeam = "Polonia", MatchDate = new DateTime(2026, 6, 16, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Arabia Saudita", AwayTeam = "Ecuador", MatchDate = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Ecuador", AwayTeam = "Polonia", MatchDate = new DateTime(2026, 6, 21, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Arabia Saudita", AwayTeam = "México", MatchDate = new DateTime(2026, 6, 21, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group B
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Estados Unidos", AwayTeam = "Gales", MatchDate = new DateTime(2026, 6, 12, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Inglaterra", AwayTeam = "Irán", MatchDate = new DateTime(2026, 6, 13, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Estados Unidos", AwayTeam = "Inglaterra", MatchDate = new DateTime(2026, 6, 18, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Irán", AwayTeam = "Gales", MatchDate = new DateTime(2026, 6, 19, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Gales", AwayTeam = "Inglaterra", MatchDate = new DateTime(2026, 6, 23, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Irán", AwayTeam = "Estados Unidos", MatchDate = new DateTime(2026, 6, 23, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group C
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Argentina", AwayTeam = "Honduras", MatchDate = new DateTime(2026, 6, 13, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Croacia", AwayTeam = "Marruecos", MatchDate = new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Argentina", AwayTeam = "Croacia", MatchDate = new DateTime(2026, 6, 19, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Marruecos", AwayTeam = "Honduras", MatchDate = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Croacia", AwayTeam = "Honduras", MatchDate = new DateTime(2026, 6, 24, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Marruecos", AwayTeam = "Argentina", MatchDate = new DateTime(2026, 6, 24, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group D
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Francia", AwayTeam = "Australia", MatchDate = new DateTime(2026, 6, 14, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Dinamarca", AwayTeam = "Túnez", MatchDate = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Francia", AwayTeam = "Dinamarca", MatchDate = new DateTime(2026, 6, 20, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Túnez", AwayTeam = "Australia", MatchDate = new DateTime(2026, 6, 21, 2, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Dinamarca", AwayTeam = "Australia", MatchDate = new DateTime(2026, 6, 25, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Túnez", AwayTeam = "Francia", MatchDate = new DateTime(2026, 6, 25, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group E
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "España", AwayTeam = "Costa Rica", MatchDate = new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Alemania", AwayTeam = "Japón", MatchDate = new DateTime(2026, 6, 15, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "España", AwayTeam = "Alemania", MatchDate = new DateTime(2026, 6, 21, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Japón", AwayTeam = "Costa Rica", MatchDate = new DateTime(2026, 6, 22, 4, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Japón", AwayTeam = "España", MatchDate = new DateTime(2026, 6, 26, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Costa Rica", AwayTeam = "Alemania", MatchDate = new DateTime(2026, 6, 26, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group F
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Bélgica", AwayTeam = "Canadá", MatchDate = new DateTime(2026, 6, 16, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Brasil", AwayTeam = "Senegal", MatchDate = new DateTime(2026, 6, 16, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Brasil", AwayTeam = "Bélgica", MatchDate = new DateTime(2026, 6, 22, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Senegal", AwayTeam = "Canadá", MatchDate = new DateTime(2026, 6, 22, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Bélgica", AwayTeam = "Senegal", MatchDate = new DateTime(2026, 6, 27, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Canadá", AwayTeam = "Brasil", MatchDate = new DateTime(2026, 6, 27, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group G
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Portugal", AwayTeam = "Nigeria", MatchDate = new DateTime(2026, 6, 12, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Uruguay", AwayTeam = "Corea del Sur", MatchDate = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Portugal", AwayTeam = "Uruguay", MatchDate = new DateTime(2026, 6, 18, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Corea del Sur", AwayTeam = "Nigeria", MatchDate = new DateTime(2026, 6, 18, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Uruguay", AwayTeam = "Nigeria", MatchDate = new DateTime(2026, 6, 23, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Corea del Sur", AwayTeam = "Portugal", MatchDate = new DateTime(2026, 6, 23, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group H
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Países Bajos", AwayTeam = "Egipto", MatchDate = new DateTime(2026, 6, 13, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Colombia", AwayTeam = "Camerún", MatchDate = new DateTime(2026, 6, 14, 4, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Países Bajos", AwayTeam = "Colombia", MatchDate = new DateTime(2026, 6, 19, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Camerún", AwayTeam = "Egipto", MatchDate = new DateTime(2026, 6, 20, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Colombia", AwayTeam = "Egipto", MatchDate = new DateTime(2026, 6, 24, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Camerún", AwayTeam = "Países Bajos", MatchDate = new DateTime(2026, 6, 24, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group I
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Italia", AwayTeam = "Ghana", MatchDate = new DateTime(2026, 6, 14, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Turquía", AwayTeam = "El Salvador", MatchDate = new DateTime(2026, 6, 15, 2, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Italia", AwayTeam = "Turquía", MatchDate = new DateTime(2026, 6, 20, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "El Salvador", AwayTeam = "Ghana", MatchDate = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Turquía", AwayTeam = "Ghana", MatchDate = new DateTime(2026, 6, 25, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "El Salvador", AwayTeam = "Italia", MatchDate = new DateTime(2026, 6, 25, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group J
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Suiza", AwayTeam = "Costa de Marfil", MatchDate = new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Serbia", AwayTeam = "Perú", MatchDate = new DateTime(2026, 6, 16, 2, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Suiza", AwayTeam = "Serbia", MatchDate = new DateTime(2026, 6, 21, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Perú", AwayTeam = "Costa de Marfil", MatchDate = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Serbia", AwayTeam = "Costa de Marfil", MatchDate = new DateTime(2026, 6, 26, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Perú", AwayTeam = "Suiza", MatchDate = new DateTime(2026, 6, 26, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group K
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Austria", AwayTeam = "Escocia", MatchDate = new DateTime(2026, 6, 16, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Argelia", AwayTeam = "Jamaica", MatchDate = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Austria", AwayTeam = "Argelia", MatchDate = new DateTime(2026, 6, 22, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Jamaica", AwayTeam = "Escocia", MatchDate = new DateTime(2026, 6, 23, 2, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Argelia", AwayTeam = "Escocia", MatchDate = new DateTime(2026, 6, 27, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Jamaica", AwayTeam = "Austria", MatchDate = new DateTime(2026, 6, 27, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Group L
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "Rumania", AwayTeam = "Venezuela", MatchDate = new DateTime(2026, 6, 17, 16, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Sudáfrica", AwayTeam = "Nueva Zelanda", MatchDate = new DateTime(2026, 6, 17, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 1 },
            new Match { Id = id++, HomeTeam = "Rumania", AwayTeam = "Sudáfrica", MatchDate = new DateTime(2026, 6, 23, 14, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Nueva Zelanda", AwayTeam = "Venezuela", MatchDate = new DateTime(2026, 6, 23, 20, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 2 },
            new Match { Id = id++, HomeTeam = "Sudáfrica", AwayTeam = "Venezuela", MatchDate = new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
            new Match { Id = id++, HomeTeam = "Nueva Zelanda", AwayTeam = "Rumania", MatchDate = new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Group, Matchday = 3 },
        ]);

        // Ronda de 32 (R32) — 16 partidos
        for (int i = 0; i < 16; i++)
            matches.Add(new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 1 + i / 4, 18 + (i % 2) * 4, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R32, Matchday = 4 });

        // Octavos de final (R16) — 8 partidos
        for (int i = 0; i < 8; i++)
            matches.Add(new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 5 + i / 2, 18 + (i % 2) * 4, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.R16, Matchday = 5 });

        // Cuartos de final (QF) — 4 partidos
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 9, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 6 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 9, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 6 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 10, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 6 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 10, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.QF, Matchday = 6 },
        ]);

        // Semifinales (SF) — 2 partidos
        matches.AddRange([
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 14, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.SF, Matchday = 7 },
            new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 15, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.SF, Matchday = 7 },
        ]);

        // Tercer puesto
        matches.Add(new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 18, 18, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.ThirdPlace, Matchday = 8 });

        // Final
        matches.Add(new Match { Id = id++, HomeTeam = "TBD", AwayTeam = "TBD", MatchDate = new DateTime(2026, 7, 19, 22, 0, 0, DateTimeKind.Utc), Phase = MatchPhase.Final, Matchday = 9 });

        return matches;
    }
}
