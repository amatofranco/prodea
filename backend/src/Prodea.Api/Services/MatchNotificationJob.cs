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
            catch (Exception ex) { logger.LogError(ex, "Error in MatchNotificationJob"); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    // Argentina does not observe daylight saving: UTC-3 year-round.
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

        // --- Match start reminder ---
        // Find any match starting in 20-40 min whose calendar day (Argentina time)
        // hasn't sent a reminder yet.
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
                await pushService.BroadcastDataAsync(db, new
                {
                    type = "match_start",
                    homeTeam = soonMatch.HomeTeam,
                    awayTeam = soonMatch.AwayTeam,
                    minutesUntil,
                    url = "/predictions"
                });
                soonMatch.ReminderSent = true;
                changed = true;
                logger.LogInformation("Start-of-day notification sent for {Day}", ToArgentinaDay(soonMatch.MatchDate).ToString("yyyy-MM-dd"));
            }
        }

        // --- End-of-day results notification ---
        // Find calendar days (Argentina time) where all matches finished
        // and the last one hasn't sent a notification yet.
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
            await pushService.BroadcastDataAsync(db, new
            {
                type = "match_end",
                homeTeam = lastMatch.HomeTeam,
                homeScore = lastMatch.HomeScore,
                awayTeam = lastMatch.AwayTeam,
                awayScore = lastMatch.AwayScore,
                url = "/tournaments"
            });
            foreach (var dm in dayMatches)
                dm.ResultNotificationSent = true;
            changed = true;
            logger.LogInformation("End-of-day notification sent for {Day}", argentinaDay.ToString("yyyy-MM-dd"));
        }

        if (changed)
            await db.SaveChangesAsync();
    }
}
