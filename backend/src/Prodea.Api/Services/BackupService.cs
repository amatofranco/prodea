using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;

namespace Prodea.Api.Services;

public class BackupService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<BackupService> logger)
    : BackgroundService
{
    private const string ResendEndpoint = "https://api.resend.com/emails";
    private static readonly TimeSpan InitialDelay       = TimeSpan.FromHours(1);
    private static readonly TimeSpan PredictionsInterval = TimeSpan.FromDays(3);
    private static readonly TimeSpan FullInterval        = TimeSpan.FromDays(7);
    private const int MaxStoredBackups = 10;

    public Task<string> RunNowAsync(CancellationToken ct = default)         => RunPredictionsBackupAsync(ct);
    public Task<string> RunFullBackupNowAsync(CancellationToken ct = default) => RunFullBackupAsync(ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        var predictionsNext = DateTime.UtcNow;
        var fullNext        = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            if (now >= predictionsNext)
            {
                try { await RunPredictionsBackupAsync(stoppingToken); }
                catch (Exception ex) { logger.LogError(ex, "Error en backup de predicciones"); }
                predictionsNext = now + PredictionsInterval;
            }

            if (now >= fullNext)
            {
                try { await RunFullBackupAsync(stoppingToken); }
                catch (Exception ex) { logger.LogError(ex, "Error en full backup"); }
                fullNext = now + FullInterval;
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    // ── Predictions backup ───────────────────────────────────────────────

    private async Task<string> RunPredictionsBackupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();

        var predictions = await db.Predictions
            .AsNoTracking()
            .Select(p => new BackupPrediction
            {
                UserId                 = p.UserId,
                MatchId                = p.MatchId,
                PredictedHomeScore     = p.PredictedHomeScore,
                PredictedAwayScore     = p.PredictedAwayScore,
                PredictedPenaltyWinner = p.PredictedPenaltyWinner,
                PointsEarned           = p.PointsEarned,
                CreatedAt              = p.CreatedAt,
                UpdatedAt              = p.UpdatedAt,
            })
            .ToListAsync(ct);

        if (predictions.Count == 0)
        {
            logger.LogInformation("Backup: sin predicciones que respaldar");
            return "no predictions to backup";
        }

        var payload = new BackupPayload
        {
            GeneratedAt = DateTime.UtcNow,
            Count       = predictions.Count,
            Predictions = predictions,
        };

        var json   = JsonSerializer.Serialize(payload, JsonOptions);
        var sizeKb = Encoding.UTF8.GetByteCount(json) / 1024.0;

        await SaveToDbAsync(db, payload, json, ct);
        logger.LogInformation("Backup predicciones en DB: {Count} ({KB:F1} KB)", predictions.Count, sizeKb);

        var (adminEmail, apiKey) = GetEmailConfig();
        var emailSent = false;
        if (adminEmail != null && apiKey != null)
        {
            await SendEmailAsync(adminEmail, apiKey,
                subject:  $"[Prodea] Predictions backup — {payload.GeneratedAt:yyyy-MM-dd}",
                heading:  "Prodea predictions backup",
                details:  $"Predictions: <strong style=\"color:#fff\">{payload.Count}</strong> &nbsp;|&nbsp; Size: <strong style=\"color:#fff\">{sizeKb:F1} KB</strong>",
                filename: $"prodea-backup-{payload.GeneratedAt:yyyy-MM-dd}.json",
                json,
                ct);
            emailSent = true;
        }

        return $"{predictions.Count} predictions backed up ({sizeKb:F1} KB)" +
               (emailSent ? $", email sent to {adminEmail}" : ", no email configured");
    }

    private async Task SaveToDbAsync(ProdeaDbContext db, BackupPayload payload, string json, CancellationToken ct)
    {
        db.PredictionBackups.Add(new PredictionBackup
        {
            CreatedAt = payload.GeneratedAt,
            Count     = payload.Count,
            JsonData  = json,
        });
        await db.SaveChangesAsync(ct);

        var old = await db.PredictionBackups
            .OrderByDescending(b => b.CreatedAt)
            .Skip(MaxStoredBackups)
            .ToListAsync(ct);
        if (old.Count > 0)
        {
            db.PredictionBackups.RemoveRange(old);
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Full backup ──────────────────────────────────────────────────────

    public async Task<string> RunFullBackupAsync(CancellationToken ct)
    {
        var (adminEmail, apiKey) = GetEmailConfig();
        if (adminEmail == null || apiKey == null)
            return "Backup__AdminEmail or Resend__ApiKey not configured";

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();

        var now = DateTime.UtcNow;
        var payload = new
        {
            generatedAt        = now,
            users              = await db.Users.AsNoTracking().ToListAsync(ct),
            tournaments        = await db.Tournaments.AsNoTracking().ToListAsync(ct),
            participants       = await db.TournamentParticipants.AsNoTracking().ToListAsync(ct),
            matches            = await db.Matches.AsNoTracking().ToListAsync(ct),
            predictions        = await db.Predictions.AsNoTracking().ToListAsync(ct),
            matchdayBadges     = await db.MatchdayBadges.AsNoTracking().ToListAsync(ct),
            accumulativeBadges = await db.AccumulativeBadges.AsNoTracking().ToListAsync(ct),
            championPicks      = await db.ChampionPicks.AsNoTracking().ToListAsync(ct),
        };

        var json     = JsonSerializer.Serialize(payload, JsonOptions);
        var sizeKb   = Encoding.UTF8.GetByteCount(json) / 1024.0;
        var filename = $"prodea-fullbackup-{now:yyyy-MM-dd}.json";

        await SendEmailAsync(adminEmail, apiKey,
            subject:  $"[Prodea] Full DB backup — {now:yyyy-MM-dd}",
            heading:  "Prodea full database backup",
            details:  $"Size: <strong style=\"color:#fff\">{sizeKb:F1} KB</strong>",
            filename,
            json,
            ct);

        logger.LogInformation("Full backup enviado: {KB:F1} KB", sizeKb);
        return $"Full backup sent to {adminEmail} ({sizeKb:F1} KB)";
    }

    // ── Shared email helper ──────────────────────────────────────────────

    private async Task SendEmailAsync(
        string to, string apiKey,
        string subject, string heading, string details,
        string filename, string json,
        CancellationToken ct)
    {
        var from = config["Resend__From"] ?? config["Resend:From"] ?? "Prodea <noreply@prodea.app>";

        var body = new
        {
            from,
            to      = new[] { to },
            subject,
            html    = $"""
                <div style="font-family:monospace;background:#0D0D0D;color:#fff;padding:24px;border-radius:8px;max-width:600px;">
                  <h2 style="color:#00FF87;margin:0 0 12px;">{heading}</h2>
                  <p style="color:#8A8A9A;margin:0 0 8px;">Date: <strong style="color:#fff">{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</strong></p>
                  <p style="color:#8A8A9A;margin:0 0 16px;">{details}</p>
                  <p style="color:#C0C0D0;">Attached as <code style="color:#00FF87">{filename}</code>.</p>
                  <p style="margin-top:16px;color:#3A3A4E;font-size:12px;">Prodea backup system.</p>
                </div>
                """,
            attachments = new[]
            {
                new { filename, content = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)) }
            },
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClientFactory.CreateClient().SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Email backup fallido: {Status} — {Error}", response.StatusCode, err);
        }
    }

    private (string? Email, string? ApiKey) GetEmailConfig() => (
        config["Backup__AdminEmail"] ?? config["Backup:AdminEmail"],
        config["Resend__ApiKey"]     ?? config["Resend:ApiKey"]);

    // ── DTOs ─────────────────────────────────────────────────────────────

    public record BackupPrediction
    {
        public int      UserId                 { get; init; }
        public int      MatchId                { get; init; }
        public int      PredictedHomeScore     { get; init; }
        public int      PredictedAwayScore     { get; init; }
        public string?  PredictedPenaltyWinner { get; init; }
        public int      PointsEarned           { get; init; }
        public DateTime CreatedAt              { get; init; }
        public DateTime UpdatedAt              { get; init; }
    }

    public record BackupPayload
    {
        public DateTime               GeneratedAt { get; init; }
        public int                    Count       { get; init; }
        public List<BackupPrediction> Predictions { get; init; } = [];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
