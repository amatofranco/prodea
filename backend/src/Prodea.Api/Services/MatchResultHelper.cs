using Prodea.Api.Models;

namespace Prodea.Api.Services;

public static class MatchResultHelper
{
    public static string? DetermineChampion(Match match)
    {
        if (match.HomeScore == null) return null;
        if (match.HomeScore > match.AwayScore) return match.HomeTeam;
        if (match.AwayScore > match.HomeScore) return match.AwayTeam;
        return match.Winner;
    }

    public static string? DetermineChampion(string? homeTeam, string? awayTeam, int? homeScore, int? awayScore, string? penaltyWinner)
    {
        if (homeScore == null) return null;
        if (homeScore > awayScore) return homeTeam;
        if (awayScore > homeScore) return awayTeam;
        return penaltyWinner;
    }
}
