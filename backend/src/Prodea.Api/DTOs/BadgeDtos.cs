namespace Prodea.Api.DTOs;

public record MatchdayBadgeDto(
    string Phase,
    int Matchday,
    string BadgeType,
    string BadgeEmoji,
    string BadgeName,
    int PointsInMatchday,
    int OccurrenceIndex,
    DateTime AwardedAt
);

public record AccumulativeBadgeDto(
    string BadgeType,
    string BadgeEmoji,
    string BadgeName,
    DateTime AwardedAt
);

public record PlayerProfileDto(
    int UserId,
    string Username,
    string? FullName,
    string? AvatarUrl,
    int TotalPoints,
    int Rank,
    List<MatchdayBadgeDto> MatchdayBadges,
    List<AccumulativeBadgeDto> AccumulativeBadges
);

public record JornadaWinnerDto(
    string Phase,
    int Matchday,
    string Label,
    int UserId,
    string Username,
    string? FullName,
    int Points
);

public record LeaderboardEntryDto(
    int Rank,
    int UserId,
    string Username,
    string? FullName,
    string? AvatarUrl,
    int TotalPoints,
    string? CurrentBadge,
    string? CurrentBadgeEmoji
);
