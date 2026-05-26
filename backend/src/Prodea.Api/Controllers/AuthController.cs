using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.DTOs;
using Prodea.Api.Models;
using Prodea.Api.Services;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ProdeaDbContext db, JwtService jwtService) : ControllerBase
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

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        return Ok(new AuthResponse(
            jwtService.GenerateToken(user),
            new UserDto(user.Id, user.Username, user.Email, user.AvatarUrl)
        ));
    }
}
