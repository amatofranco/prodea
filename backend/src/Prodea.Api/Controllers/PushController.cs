using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController(ProdeaDbContext db, IConfiguration config) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("public-key")]
    public IActionResult GetPublicKey()
    {
        var key = config["Vapid:PublicKey"] ?? throw new InvalidOperationException("Vapid:PublicKey no configurado");
        return Ok(new { publicKey = key });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var userId = CurrentUserId;
        var existing = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);
        if (existing != null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UserId = userId;
        }
        else
        {
            db.PushSubscriptions.Add(new UserPushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
            });
        }
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);
        if (sub != null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync();
        }
        return Ok();
    }
}

public record SubscribeRequest(string Endpoint, string P256dh, string Auth);
public record UnsubscribeRequest(string Endpoint);
