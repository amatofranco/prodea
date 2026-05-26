using System.ComponentModel.DataAnnotations;

namespace Prodea.Api.DTOs;

public record CreateTournamentRequest(
    [Required, MinLength(3), MaxLength(100)] string Name
);

public record JoinTournamentRequest(
    [Required] string CodeOrInviteLink
);

public record TournamentDto(
    int Id,
    string Name,
    string Code,
    string InviteLink,
    int AdminUserId,
    string AdminUsername,
    int ParticipantCount,
    DateTime CreatedAt
);

public record TournamentDetailDto(
    int Id,
    string Name,
    string Code,
    string InviteLink,
    int AdminUserId,
    string AdminUsername,
    List<ParticipantDto> Participants,
    DateTime CreatedAt
);

public record ParticipantDto(
    int UserId,
    string Username,
    string? AvatarUrl,
    int TotalPoints,
    int Rank,
    string? LastBadge
);
