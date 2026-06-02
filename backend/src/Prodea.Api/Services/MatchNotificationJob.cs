using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class MatchNotificationJob(IServiceScopeFactory scopeFactory, ILogger<MatchNotificationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(); }
            catch (Exception ex) { logger.LogError(ex, "Error en MatchNotificationJob"); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task RunAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

        var now = DateTime.UtcNow;

        // Recordatorio: partidos que arrancan entre 55 y 65 minutos
        var upcoming = await db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled
                && !m.ReminderSent
                && m.MatchDate > now.AddMinutes(55)
                && m.MatchDate <= now.AddMinutes(65))
            .ToListAsync();

        foreach (var match in upcoming)
        {
            var title = "⚽ ¡Cargá tu predicción!";
            var body = $"{match.HomeTeam} vs {match.AwayTeam} arranca en ~1 hora";
            await SendToAllAsync(db, pushService, title, body, "/predicciones");
            match.ReminderSent = true;
            logger.LogInformation("Recordatorio enviado para partido {MatchId}", match.Id);
        }

        // Resultado: partidos que acaban de terminar
        var finished = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished && !m.ResultNotificationSent)
            .ToListAsync();

        foreach (var match in finished)
        {
            var title = "🏁 Resultado final";
            var body = $"{match.HomeTeam} {match.HomeScore} - {match.AwayScore} {match.AwayTeam}";
            await SendToAllAsync(db, pushService, title, body, "/predicciones");
            match.ResultNotificationSent = true;
            logger.LogInformation("Notificación de resultado enviada para partido {MatchId}", match.Id);
        }

        if (upcoming.Any() || finished.Any())
            await db.SaveChangesAsync();
    }

    private async Task SendToAllAsync(ProdeaDbContext db, PushNotificationService pushService, string title, string body, string url)
    {
        var subscriptions = await db.PushSubscriptions.ToListAsync();
        var expired = new List<UserPushSubscription>();

        foreach (var sub in subscriptions)
        {
            try { await pushService.SendToUserAsync(sub, title, body, url); }
            catch (ExpiredSubscriptionException) { expired.Add(sub); }
            catch { }
        }

        if (expired.Any())
        {
            db.PushSubscriptions.RemoveRange(expired);
            await db.SaveChangesAsync();
        }
    }
}
