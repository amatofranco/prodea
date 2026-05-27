using System.Security.Cryptography;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ProdeaDbContext db,
    JwtService jwtService,
    EmailService emailService,
    IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "El nombre de usuario ya está en uso" });

        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "El email ya está registrado" });

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(
            jwtService.GenerateToken(user),
            new UserDto(user.Id, user.Username, user.Email, user.AvatarUrl)
        ));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

        if (user == null || user.PasswordHash == null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        return Ok(new AuthResponse(
            jwtService.GenerateToken(user),
            new UserDto(user.Id, user.Username, user.Email, user.AvatarUrl)
        ));
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [config["Google:ClientId"]]
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch
        {
            return Unauthorized(new { message = "Token de Google inválido" });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

        if (user == null)
        {
            // Check if an account with this email already exists
            user = await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (user != null)
            {
                // Link Google to existing account
                user.GoogleId = payload.Subject;
                if (user.AvatarUrl == null && payload.Picture != null)
                    user.AvatarUrl = payload.Picture;
            }
            else
            {
                // Create new account
                var username = await GenerateUniqueUsernameAsync(payload.Email);
                user = new User
                {
                    Username = username,
                    Email = payload.Email,
                    GoogleId = payload.Subject,
                    AvatarUrl = payload.Picture,
                };
                db.Users.Add(user);
            }
            await db.SaveChangesAsync();
        }

        return Ok(new AuthResponse(
            jwtService.GenerateToken(user),
            new UserDto(user.Id, user.Username, user.Email, user.AvatarUrl)
        ));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        // Always return 200 to avoid email enumeration
        if (user == null || user.PasswordHash == null)
            return Ok(new { message = "Si el email existe, te enviamos un link para recuperar tu contraseña." });

        // Invalidate existing tokens for this user
        var existing = db.PasswordResetTokens.Where(t => t.UserId == user.Id && !t.Used);
        db.PasswordResetTokens.RemoveRange(existing);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        await emailService.SendPasswordResetAsync(user.Email, token);

        return Ok(new { message = "Si el email existe, te enviamos un link para recuperar tu contraseña." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token && !t.Used);

        if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "El link de recuperación es inválido o expiró." });

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetToken.Used = true;
        await db.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada correctamente." });
    }

    private async Task<string> GenerateUniqueUsernameAsync(string email)
    {
        var base_ = email.Split('@')[0]
            .Replace(".", "")
            .Replace("-", "")
            .Replace("_", "");
        base_ = base_[..Math.Min(base_.Length, 20)];

        if (!await db.Users.AnyAsync(u => u.Username == base_))
            return base_;

        for (var i = 1; i < 1000; i++)
        {
            var candidate = $"{base_}{i}";
            if (!await db.Users.AnyAsync(u => u.Username == candidate))
                return candidate;
        }

        return base_ + RandomNumberGenerator.GetInt32(1000, 9999);
    }
}
