using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly TimeSpan InitialDelay   = TimeSpan.FromHours(1);
    private static readonly TimeSpan BackupInterval = TimeSpan.FromDays(3);
    private const int MaxStoredBackups = 10;

    public Task<string> RunNowAsync(CancellationToken ct = default) => RunBackupAsync(ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunBackupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ejecutando backup de predicciones");
            }

            await Task.Delay(BackupInterval, stoppingToken);
        }
    }

    private async Task<string> RunBackupAsync(CancellationToken ct)
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

        logger.LogInformation("Backup guardado en DB: {Count} predicciones ({KB:F1} KB)", predictions.Count, sizeKb);

        var adminEmail = config["Backup:AdminEmail"];
        var apiKey     = config["Resend:ApiKey"];
        var emailSent  = false;
        if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(apiKey))
        {
            await SendBackupEmailAsync(adminEmail, apiKey, payload, json, sizeKb, ct);
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

    private async Task SendBackupEmailAsync(
        string adminEmail, string apiKey,
        BackupPayload payload, string json, double sizeKb,
        CancellationToken ct)
    {
        try
        {
            var from     = config["Resend:From"] ?? "Prodea <noreply@prodea.app>";
            var filename = $"prodea-backup-{payload.GeneratedAt:yyyy-MM-dd}.json";

            var body = new
            {
                from,
                to      = new[] { adminEmail },
                subject = $"[Prodea] Predictions backup — {payload.GeneratedAt:yyyy-MM-dd}",
                html    = $"""
                    <div style="font-family:monospace;background:#0D0D0D;color:#fff;padding:24px;border-radius:8px;max-width:600px;">
                      <h2 style="color:#00FF87;margin:0 0 12px;">Prodea predictions backup</h2>
                      <p style="color:#8A8A9A;margin:0 0 8px;">Date: <strong style="color:#fff">{payload.GeneratedAt:yyyy-MM-dd HH:mm} UTC</strong></p>
                      <p style="color:#8A8A9A;margin:0 0 16px;">Predictions: <strong style="color:#fff">{payload.Count}</strong> &nbsp;|&nbsp; Size: <strong style="color:#fff">{sizeKb:F1} KB</strong></p>
                      <p style="color:#C0C0D0;">Full backup attached as <code style="color:#00FF87">{filename}</code>.</p>
                      <p style="margin-top:16px;color:#3A3A4E;font-size:12px;">Auto-generated by Prodea.</p>
                    </div>
                    """,
                attachments = new[]
                {
                    new
                    {
                        filename,
                        content = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
                    }
                },
            };

            var client  = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Backup email failed: {Status} — {Error}", response.StatusCode, err);
            }
            else
            {
                logger.LogInformation("Backup email sent to {Email}", adminEmail);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send backup email (backup was saved to DB)");
        }
    }

    // DTO compartido con AdminController para deserializar
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
