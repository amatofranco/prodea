namespace Prodea.Api.DTOs;

public record MatchdayBadgeDto(
    int Matchday,
    string BadgeType,
    string BadgeEmoji,
    string BadgeName,
    int PointsInMatchday,
    string RandomPhrase,
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
    string? AvatarUrl,
    int TotalPoints,
    int Rank,
    List<MatchdayBadgeDto> MatchdayBadges,
    List<AccumulativeBadgeDto> AccumulativeBadges
);

public record LeaderboardEntryDto(
    int Rank,
    int UserId,
    string Username,
    string? AvatarUrl,
    int TotalPoints,
    string? CurrentBadge,
    string? CurrentBadgeEmoji
);
