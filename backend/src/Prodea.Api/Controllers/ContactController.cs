using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/contact")]
[Authorize]
public class ContactController(ProdeaDbContext db, EmailService emailService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 500)
            return BadRequest(new { message = "invalid_message_length" });

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return Unauthorized();

        var lang = ParseLang(Request.Headers["Accept-Language"]);
        await emailService.SendContactMessageAsync(user.Username, user.Email, request.Message.Trim(), lang);
        return Ok();
    }

    private static string ParseLang(string? acceptLanguage)
    {
        if (string.IsNullOrEmpty(acceptLanguage)) return "es";
        return acceptLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "es";
    }
}

public record ContactRequest([MaxLength(500)] string Message);
