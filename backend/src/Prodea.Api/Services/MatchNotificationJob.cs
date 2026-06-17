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
        var changed = false;

        // --- Notificación de inicio ---
        // Buscar cualquier partido en los próximos 20-40 min cuya jornada todavía no mandó reminder.
        // Usar Matchday (no día calendario) para que un partido nocturno de jornada anterior
        // no dispare un reminder de "nueva jornada".
        var soonMatch = await db.Matches
            .Where(m => m.Matchday != null
                     && m.Status == MatchStatus.Scheduled
                     && m.MatchDate > now.AddMinutes(20)
                     && m.MatchDate <= now.AddMinutes(40))
            .OrderBy(m => m.MatchDate)
            .FirstOrDefaultAsync();

        if (soonMatch != null)
        {
            var reminderAlreadySent = await db.Matches
                .AnyAsync(m => m.Matchday == soonMatch.Matchday && m.ReminderSent);

            if (!reminderAlreadySent)
            {
                var minutesUntil = (int)(soonMatch.MatchDate - now).TotalMinutes;
                var title = $"⚽ {soonMatch.HomeTeam} vs {soonMatch.AwayTeam}";
                var body = $"Arranca en {minutesUntil} min. ¿Tenés tus predicciones cargadas?";
                await SendToAllAsync(db, pushService, title, body, "/predicciones");
                soonMatch.ReminderSent = true;
                changed = true;
                logger.LogInformation("Notificación de inicio de jornada {Matchday} enviada", soonMatch.Matchday);
            }
        }

        // --- Notificación de cierre ---
        // Buscar jornadas donde todos los partidos terminaron y el último no mandó notificación.
        // Al agrupar por Matchday en vez de día calendario, un partido nocturno cierra
        // correctamente su propia jornada y no la del día siguiente.
        var matchdaysWithUnsent = await db.Matches
            .Where(m => m.Matchday != null
                     && m.Status == MatchStatus.Finished
                     && !m.ResultNotificationSent)
            .Select(m => m.Matchday)
            .Distinct()
            .ToListAsync();

        foreach (var matchday in matchdaysWithUnsent)
        {
            var mdMatches = await db.Matches
                .Where(m => m.Matchday == matchday)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            if (!mdMatches.All(m => m.Status == MatchStatus.Finished)) continue;

            var lastMatch = mdMatches.Last();
            if (lastMatch.ResultNotificationSent) continue;

            var title = "🏁 Se terminó la jornada";
            var body = $"Último resultado: {lastMatch.HomeTeam} {lastMatch.HomeScore}-{lastMatch.AwayScore} {lastMatch.AwayTeam}. Mirá cómo quedó la tabla.";
            await SendToAllAsync(db, pushService, title, body, "/torneos");
            lastMatch.ResultNotificationSent = true;
            changed = true;
            logger.LogInformation("Notificación de cierre de jornada {Matchday} enviada", matchday);
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
