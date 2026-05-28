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
    int? Minute
);

public record PredictionDto(
    int Id,
    int PredictedHomeScore,
    int PredictedAwayScore,
    int PointsEarned
);

public record SubmitPredictionRequest(
    [Required, Range(0, 9)] int PredictedHomeScore,
    [Required, Range(0, 9)] int PredictedAwayScore
);

public record UpdateMatchResultRequest(
    [Required, Range(0, 99)] int HomeScore,
    [Required, Range(0, 99)] int AwayScore
);
