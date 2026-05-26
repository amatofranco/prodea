using System.ComponentModel.DataAnnotations;

namespace Prodea.Api.DTOs;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);

public record LoginRequest(
    [Required] string UsernameOrEmail,
    [Required] string Password
);

public record AuthResponse(
    string Token,
    UserDto User
);

public record UserDto(
    int Id,
    string Username,
    string Email,
    string? AvatarUrl
);
