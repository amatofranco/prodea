using System.ComponentModel.DataAnnotations;

namespace Prodea.Api.DTOs;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50), RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "El nombre de usuario solo puede contener letras, números y guión bajo")] string Username,
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

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword
);

public record GoogleLoginRequest(
    [Required] string IdToken
);
