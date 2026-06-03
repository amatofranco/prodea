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

        // Usar hora de Argentina (UTC-3)
        var now = DateTime.UtcNow;
        var todayUtc = now.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var todayMatches = await db.Matches
            .Where(m => m.MatchDate >= todayUtc && m.MatchDate < tomorrowUtc)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        if (!todayMatches.Any()) return;

        var changed = false;

        // Notificación de inicio: 20-40 min antes del primer partido del día
        var firstMatch = todayMatches.First();
        if (!firstMatch.ReminderSent
            && firstMatch.Status == MatchStatus.Scheduled
            && firstMatch.MatchDate > now.AddMinutes(20)
            && firstMatch.MatchDate <= now.AddMinutes(40))
        {
            var minutesUntil = (int)(firstMatch.MatchDate - now).TotalMinutes;
            var title = $"⚽ {firstMatch.HomeTeam} vs {firstMatch.AwayTeam}";
            var body = $"Arranca en {minutesUntil} min. ¿Tenés tu predicción cargada?";
            await SendToAllAsync(db, pushService, title, body, "/predicciones");
            firstMatch.ReminderSent = true;
            changed = true;
            logger.LogInformation("Notificación de inicio de jornada enviada");
        }

        // Notificación de cierre: cuando termina el último partido del día
        var lastMatch = todayMatches.Last();
        if (!lastMatch.ResultNotificationSent
            && todayMatches.All(m => m.Status == MatchStatus.Finished))
        {
            var title = "🏁 Se terminó la jornada";
            var body = $"Último resultado: {lastMatch.HomeTeam} {lastMatch.HomeScore}-{lastMatch.AwayScore} {lastMatch.AwayTeam}. Mirá cómo quedó la tabla.";
            await SendToAllAsync(db, pushService, title, body, "/torneos");
            lastMatch.ResultNotificationSent = true;
            changed = true;
            logger.LogInformation("Notificación de cierre de jornada enviada");
        }

        if (changed)
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
