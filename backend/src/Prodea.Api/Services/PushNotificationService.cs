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
}

public class ExpiredSubscriptionException : Exception { }
