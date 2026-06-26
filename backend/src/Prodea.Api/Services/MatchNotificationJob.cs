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

    // Argentina no usa horario de verano: UTC-3 todo el año.
    private const int ArgentinaUtcOffsetHours = -3;

    private static DateTime ToArgentinaDay(DateTime matchDateUtc) => matchDateUtc.AddHours(ArgentinaUtcOffsetHours).Date;

    private static (DateTime FromUtc, DateTime ToUtc) ArgentinaDayBoundsUtc(DateTime argentinaDay)
    {
        var fromUtc = argentinaDay.AddHours(-ArgentinaUtcOffsetHours);
        return (fromUtc, fromUtc.AddDays(1));
    }

    private async Task RunAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

        var now = DateTime.UtcNow;
        var changed = false;

        // --- Notificación de inicio ---
        // Buscar cualquier partido en los próximos 20-40 min cuyo día calendario (hora Argentina)
        // todavía no mandó reminder.
        var soonMatch = await db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled
                     && m.MatchDate > now.AddMinutes(20)
                     && m.MatchDate <= now.AddMinutes(40))
            .OrderBy(m => m.MatchDate)
            .FirstOrDefaultAsync();

        if (soonMatch != null)
        {
            var (fromUtc, toUtc) = ArgentinaDayBoundsUtc(ToArgentinaDay(soonMatch.MatchDate));

            var reminderAlreadySent = await db.Matches
                .AnyAsync(m => m.MatchDate >= fromUtc && m.MatchDate < toUtc && m.ReminderSent);

            if (!reminderAlreadySent)
            {
                var minutesUntil = (int)(soonMatch.MatchDate - now).TotalMinutes;
                var title = $"⚽ {soonMatch.HomeTeam} vs {soonMatch.AwayTeam}";
                var body = $"Arranca en {minutesUntil} min. ¿Tenés tus predicciones cargadas?";
                await SendToAllAsync(db, pushService, title, body, "/predicciones");
                soonMatch.ReminderSent = true;
                changed = true;
                logger.LogInformation("Notificación de inicio del día {Day} enviada", ToArgentinaDay(soonMatch.MatchDate).ToString("yyyy-MM-dd"));
            }
        }

        // --- Notificación de cierre ---
        // Buscar días calendario (hora Argentina) donde todos los partidos terminaron
        // y el último no mandó notificación.
        var unsentFinishedDates = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished && !m.ResultNotificationSent)
            .Select(m => m.MatchDate)
            .ToListAsync();

        var pendingDays = unsentFinishedDates.Select(ToArgentinaDay).Distinct();

        foreach (var argentinaDay in pendingDays)
        {
            var (fromUtc, toUtc) = ArgentinaDayBoundsUtc(argentinaDay);

            var dayMatches = await db.Matches
                .Where(m => m.MatchDate >= fromUtc && m.MatchDate < toUtc)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            if (dayMatches.Count == 0 || !dayMatches.All(m => m.Status == MatchStatus.Finished)) continue;

            if (dayMatches.Any(m => m.ResultNotificationSent)) continue;

            var lastMatch = dayMatches.Last();
            var title = "🏁 Se terminaron los partidos de hoy";
            var body = $"Último resultado: {lastMatch.HomeTeam} {lastMatch.HomeScore}-{lastMatch.AwayScore} {lastMatch.AwayTeam}. Mirá cómo quedó la tabla.";
            await SendToAllAsync(db, pushService, title, body, "/torneos");
            foreach (var dm in dayMatches)
                dm.ResultNotificationSent = true;
            changed = true;
            logger.LogInformation("Notificación de cierre del día {Day} enviada", argentinaDay.ToString("yyyy-MM-dd"));
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
