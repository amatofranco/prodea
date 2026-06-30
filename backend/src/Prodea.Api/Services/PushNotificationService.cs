using WebPush;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Prodea.Api.Services;

public class PushNotificationService(IConfiguration config, ILogger<PushNotificationService> logger)
{
    private readonly string _publicKey = config["Vapid:PublicKey"] ?? throw new InvalidOperationException("Vapid:PublicKey no configurado");
    private readonly string _privateKey = config["Vapid:PrivateKey"] ?? throw new InvalidOperationException("Vapid:PrivateKey no configurado");
    private readonly string _subject = config["Vapid:Subject"] ?? "mailto:noreply@prodea.app";

    public async Task SendToUserAsync(UserPushSubscription sub, string title, string body, string url = "/")
    {
        var client = new WebPushClient();
        client.SetVapidDetails(_subject, _publicKey, _privateKey);

        var subscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body, url });

        try
        {
            await client.SendNotificationAsync(subscription, payload);
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Suscripción expirada para endpoint {Endpoint}", sub.Endpoint);
            throw new ExpiredSubscriptionException();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error enviando push a {Endpoint}", sub.Endpoint);
            throw;
        }
    }

    public async Task SendDataAsync(UserPushSubscription sub, object data)
    {
        var client = new WebPushClient();
        client.SetVapidDetails(_subject, _publicKey, _privateKey);

        var subscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
        var payload = System.Text.Json.JsonSerializer.Serialize(data);

        try
        {
            await client.SendNotificationAsync(subscription, payload);
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ExpiredSubscriptionException();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error sending push to {Endpoint}", sub.Endpoint);
            throw;
        }
    }

    public async Task<int> BroadcastAsync(ProdeaDbContext db, string title, string body, string url = "/")
    {
        var subscriptions = await db.PushSubscriptions.ToListAsync();
        var sent = 0;
        var expired = new List<UserPushSubscription>();

        foreach (var sub in subscriptions)
        {
            try { await SendToUserAsync(sub, title, body, url); sent++; }
            catch (ExpiredSubscriptionException) { expired.Add(sub); }
            catch { }
        }

        if (expired.Count > 0)
        {
            db.PushSubscriptions.RemoveRange(expired);
            await db.SaveChangesAsync();
        }

        return sent;
    }
}

public class ExpiredSubscriptionException : Exception { }
