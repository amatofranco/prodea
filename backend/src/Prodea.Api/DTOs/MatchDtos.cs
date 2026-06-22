using System.ComponentModel.DataAnnotations;
using Prodea.Api.Models;

namespace Prodea.Api.DTOs;

public record MatchDto(
    int Id,
    string HomeTeam,
    string AwayTeam,
    string? HomeTeamLabel,
    string? AwayTeamLabel,
    string? HomeTeamFlag,
    string? AwayTeamFlag,
    DateTime MatchDate,
    string Phase,
    int? Matchday,
    int? HomeScore,
    int? AwayScore,
    string Status
);

public record GoalDto(string Scorer, string Team, string Minute);

public record MatchWithPredictionDto(
    int Id,
    string HomeTeam,
    string AwayTeam,
    string? HomeTeamLabel,
    string? AwayTeamLabel,
    string? HomeTeamFlag,
    string? AwayTeamFlag,
    DateTime MatchDate,
    string Phase,
    int? Matchday,
    int? HomeScore,
    int? AwayScore,
    string Status,
    PredictionDto? UserPrediction,
    int? Minute,
    List<GoalDto>? Goals = null,
    string? LivePhase = null,
    string? UserChampionPick = null,
    int? UserChampionPickPoints = null
);

public record PredictionDto(
    int Id,
    int PredictedHomeScore,
    int PredictedAwayScore,
    int PointsEarned,
    string? PredictedPenaltyWinner = null
);

public record SubmitPredictionRequest(
    [Required, Range(0, 9)] int PredictedHomeScore,
    [Required, Range(0, 9)] int PredictedAwayScore,
    string? PredictedPenaltyWinner = null
);

public record UpdateMatchResultRequest(
    [Required, Range(0, 99)] int HomeScore,
    [Required, Range(0, 99)] int AwayScore,
    string? Winner = null  // "home" | "away" for penalty matches where scores are tied
);

public record MatchPredictionDto(
    int UserId,
    string Username,
    string? FullName,
    int? PredictedHomeScore,
    int? PredictedAwayScore,
    int PointsEarned,
    string? PredictedPenaltyWinner = null,
    string? ChampionPick = null,
    int? ChampionPickPoints = null
);
