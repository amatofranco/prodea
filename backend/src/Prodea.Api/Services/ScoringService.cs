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
            // En un partido que fue a penales el resultado completo es: score 90' + ganador por penales.
            // 3 pts: acertó el score de 90 min Y acertó quién ganó por penales.
            // 1 pt:  acertó el score de 90 min (sin pick o pick equivocado), O predijo el ganador correcto.
            bool exactScore = prediction.PredictedHomeScore == actualHome && prediction.PredictedAwayScore == actualAway;

            // Ganador que el usuario predijo (home/away/null si predijo draw sin pick de penales)
            string? predictedWinner =
                prediction.PredictedHomeScore > prediction.PredictedAwayScore ? "home" :
                prediction.PredictedHomeScore < prediction.PredictedAwayScore ? "away" :
                prediction.PredictedPenaltyWinner; // draw predicho → usa el pick de penales

            if (exactScore && predictedWinner == winnerSide) return 3;
            if (exactScore) return 1;
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
