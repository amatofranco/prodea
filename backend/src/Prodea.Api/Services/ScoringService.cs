using Prodea.Api.Models;

namespace Prodea.Api.Services;

public static class ScoringService
{
    // winnerSide = "home" | "away" when match went to penalties (scores tied but there's a winner)
    public static int CalculatePoints(Prediction prediction, int actualHome, int actualAway, string? winnerSide = null)
    {
        bool isPenaltyMatch = actualHome == actualAway && winnerSide != null;

        if (isPenaltyMatch)
        {
            // Partido decidido por penales: el resultado "completo" es score exacto + ganador por penales.
            // 3 pts: score exacto Y acertó quién ganó por penales.
            // 1 pt:  score exacto (sin pick o pick equivocado),
            //        O predijo empate (cualquier marcador) → acertó que iría a penales,
            //        O predijo el ganador correcto con un score no empatado (ej. 2-1 home).
            bool exactScore = prediction.PredictedHomeScore == actualHome && prediction.PredictedAwayScore == actualAway;
            bool predictedDrawScore = prediction.PredictedHomeScore == prediction.PredictedAwayScore;

            string? predictedWinner =
                prediction.PredictedHomeScore > prediction.PredictedAwayScore ? "home" :
                prediction.PredictedHomeScore < prediction.PredictedAwayScore ? "away" :
                prediction.PredictedPenaltyWinner; // draw → usa el pick de penales

            if (exactScore && predictedWinner == winnerSide) return 3;
            if (exactScore) return 1;
            if (predictedDrawScore) return 1; // cualquier empate predicho → acertó que habría penales
            if (predictedWinner == winnerSide) return 1;
            return 0;
        }

        // Partido normal (sin penales)
        if (prediction.PredictedHomeScore == actualHome && prediction.PredictedAwayScore == actualAway)
            return 3;

        bool predictedHomeWin = prediction.PredictedHomeScore > prediction.PredictedAwayScore;
        bool predictedDraw    = prediction.PredictedHomeScore == prediction.PredictedAwayScore;
        bool predictedAwayWin = prediction.PredictedHomeScore < prediction.PredictedAwayScore;
        bool actualHomeWin    = actualHome > actualAway;
        bool actualDraw       = actualHome == actualAway;
        bool actualAwayWin    = actualHome < actualAway;

        if ((predictedHomeWin && actualHomeWin) || (predictedDraw && actualDraw) || (predictedAwayWin && actualAwayWin))
            return 1;

        return 0;
    }
}
