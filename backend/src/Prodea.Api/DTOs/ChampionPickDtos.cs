using System.ComponentModel.DataAnnotations;

namespace Prodea.Api.DTOs;

public record ChampionPickStatusDto(
    string? MyPick,
    bool IsLocked,
    DateTime LockTime,
    string? Champion,
    List<ParticipantPickDto> AllPicks,
    List<string> AvailableTeams
);

public record ParticipantPickDto(
    int UserId,
    string Username,
    string? CountryName,
    bool CorrectPick
);

public record SubmitChampionPickRequest(
    [Required, MaxLength(100)] string CountryName
);
