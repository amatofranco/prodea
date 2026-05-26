using Prodea.Api.Models;

namespace Prodea.Api.Services;

public static class ScoringService
{
    public static int CalculatePoints(Prediction prediction, int actualHome, int actualAway)
    {
        if (prediction.PredictedHomeScore == actualHome && prediction.PredictedAwayScore == actualAway)
            return 3;

        bool predictedHomeWin = prediction.PredictedHomeScore > prediction.PredictedAwayScore;
        bool predictedDraw = prediction.PredictedHomeScore == prediction.PredictedAwayScore;
        bool predictedAwayWin = prediction.PredictedHomeScore < prediction.PredictedAwayScore;

        bool actualHomeWin = actualHome > actualAway;
        bool actualDraw = actualHome == actualAway;
        bool actualAwayWin = actualHome < actualAway;

        if ((predictedHomeWin && actualHomeWin) || (predictedDraw && actualDraw) || (predictedAwayWin && actualAwayWin))
            return 1;

        return 0;
    }
}
