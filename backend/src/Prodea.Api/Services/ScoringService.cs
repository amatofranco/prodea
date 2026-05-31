using Prodea.Api.Models;

namespace Prodea.Api.Services;

public static class ScoringService
{
    // Score siempre se evalúa en los 90' (regularTime).
    // winnerSide = "home" | "away" — quién pasó de ronda (ET o penales); solo en knockout con empate a 90'.
    // +2 bonus: predijo empate Y acertó quién pasa. Stackeable con el exacto → máximo 5 pts.
    public static int CalculatePoints(Prediction prediction, int actualHome, int actualAway, string? winnerSide = null)
    {
        bool exactScore    = prediction.PredictedHomeScore == actualHome && prediction.PredictedAwayScore == actualAway;
        bool predictedDraw = prediction.PredictedHomeScore == prediction.PredictedAwayScore;
        bool actualDraw    = actualHome == actualAway;

        int points;
        if (exactScore)
        {
            points = 3;
        }
        else
        {
            bool predictedHomeWin = prediction.PredictedHomeScore > prediction.PredictedAwayScore;
            bool predictedAwayWin = prediction.PredictedHomeScore < prediction.PredictedAwayScore;
            bool actualHomeWin    = actualHome > actualAway;
            bool actualAwayWin    = actualHome < actualAway;

            points = ((predictedHomeWin && actualHomeWin) || (predictedDraw && actualDraw) || (predictedAwayWin && actualAwayWin))
                ? 1 : 0;
        }

        // Bonus +2: predijo empate y acertó quién avanza (penales o alargue)
        if (predictedDraw && actualDraw && winnerSide != null && prediction.PredictedPenaltyWinner == winnerSide)
            points += 2;

        return points;
    }
}
