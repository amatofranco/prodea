using Prodea.Api.Models;

namespace Prodea.Api.Services;

public static class ScoringService
{
    // winnerSide = "home" | "away" when match went to penalties (scores tied but there's a winner)
    public static int CalculatePoints(Prediction prediction, int actualHome, int actualAway, string? winnerSide = null)
    {
        if (prediction.PredictedHomeScore == actualHome && prediction.PredictedAwayScore == actualAway)
            return 3;

        bool predictedHomeWin = prediction.PredictedHomeScore > prediction.PredictedAwayScore;
        bool predictedDraw = prediction.PredictedHomeScore == prediction.PredictedAwayScore;
        bool predictedAwayWin = prediction.PredictedHomeScore < prediction.PredictedAwayScore;

        bool actualHomeWin, actualDraw, actualAwayWin;
        if (actualHome == actualAway && winnerSide != null)
        {
            actualHomeWin = winnerSide == "home";
            actualDraw = false;
            actualAwayWin = winnerSide == "away";
        }
        else
        {
            actualHomeWin = actualHome > actualAway;
            actualDraw = actualHome == actualAway;
            actualAwayWin = actualHome < actualAway;
        }

        if ((predictedHomeWin && actualHomeWin) || (predictedDraw && actualDraw) || (predictedAwayWin && actualAwayWin))
            return 1;

        // Empate predicho en partido eliminatorio que fue a penales: 1 pt si acertó el ganador por penales
        if (predictedDraw && winnerSide != null && prediction.PredictedPenaltyWinner == winnerSide)
            return 1;

        return 0;
    }
}
