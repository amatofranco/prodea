using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Models;
using Resend;

namespace Prodea.Api.Services;

public class BackupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<BackupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan InitialDelay   = TimeSpan.FromHours(1);
    private static readonly TimeSpan BackupInterval = TimeSpan.FromDays(3);
    private const int MaxStoredBackups = 10;

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

    private async Task RunBackupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();

        var predictions = await db.Predictions
            .AsNoTracking()
            .Select(p => new BackupPrediction
            {
                UserId             = p.UserId,
                MatchId            = p.MatchId,
                PredictedHomeScore = p.PredictedHomeScore,
                PredictedAwayScore = p.PredictedAwayScore,
                PointsEarned       = p.PointsEarned,
                CreatedAt          = p.CreatedAt,
                UpdatedAt          = p.UpdatedAt,
            })
            .ToListAsync(ct);

        if (predictions.Count == 0)
        {
            logger.LogInformation("Backup: sin predicciones que respaldar");
            return;
        }

        var payload = new BackupPayload
        {
            GeneratedAt = DateTime.UtcNow,
            Count       = predictions.Count,
            Predictions = predictions,
        };

        var json    = JsonSerializer.Serialize(payload, JsonOptions);
        var sizeKb  = Encoding.UTF8.GetByteCount(json) / 1024.0;

        // Guarda en DB
        db.PredictionBackups.Add(new PredictionBackup
        {
            CreatedAt = payload.GeneratedAt,
            Count     = payload.Count,
            JsonData  = json,
        });
        await db.SaveChangesAsync(ct);

        // Elimina backups viejos, conserva solo los últimos MaxStoredBackups
        var old = await db.PredictionBackups
            .OrderByDescending(b => b.CreatedAt)
            .Skip(MaxStoredBackups)
            .ToListAsync(ct);
        if (old.Count > 0)
        {
            db.PredictionBackups.RemoveRange(old);
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("Backup guardado en DB: {Count} predicciones ({KB:F1} KB)", predictions.Count, sizeKb);

        // Manda email si está configurado
        var adminEmail = config["Backup:AdminEmail"];
        if (!string.IsNullOrEmpty(adminEmail))
        {
            try
            {
                var from   = config["Resend:From"] ?? "Prodeá <noreply@prodea.app>";
                var resend = scope.ServiceProvider.GetRequiredService<IResend>();

                var preview = json.Length > 8000 ? json[..8000] + "\n... (truncado)" : json;
                var message = new EmailMessage
                {
                    From     = from,
                    To       = { adminEmail },
                    Subject  = $"[Prodeá] Backup predicciones — {payload.GeneratedAt:yyyy-MM-dd}",
                    HtmlBody = $"""
                        <div style="font-family:monospace;background:#0D0D0D;color:#fff;padding:24px;border-radius:8px;max-width:600px;">
                          <h2 style="color:#00FF87;margin:0 0 12px;">Backup predicciones Prodeá</h2>
                          <p style="color:#8A8A9A;margin:0 0 8px;">Fecha: <strong style="color:#fff">{payload.GeneratedAt:yyyy-MM-dd HH:mm} UTC</strong></p>
                          <p style="color:#8A8A9A;margin:0 0 16px;">Predicciones: <strong style="color:#fff">{predictions.Count}</strong> &nbsp;|&nbsp; Tamaño: <strong style="color:#fff">{sizeKb:F1} KB</strong></p>
                          <pre style="background:#1A1A2E;padding:16px;border-radius:6px;color:#8A8A9A;font-size:11px;overflow-x:auto;max-height:400px;">{preview}</pre>
                          <p style="margin-top:16px;color:#3A3A4E;font-size:12px;">Generado automáticamente por Prodeá.</p>
                        </div>
                        """,
                    TextBody = json,
                };

                await resend.EmailSendAsync(message);
                logger.LogInformation("Backup enviado por email a {Email}", adminEmail);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo enviar el email de backup (el backup en DB sí se guardó)");
            }
        }
    }

    // DTO compartido con AdminController para deserializar
    public record BackupPrediction
    {
        public int      UserId             { get; init; }
        public int      MatchId            { get; init; }
        public int      PredictedHomeScore { get; init; }
        public int      PredictedAwayScore { get; init; }
        public int      PointsEarned       { get; init; }
        public DateTime CreatedAt          { get; init; }
        public DateTime UpdatedAt          { get; init; }
    }

    public record BackupPayload
    {
        public DateTime              GeneratedAt { get; init; }
        public int                   Count       { get; init; }
        public List<BackupPrediction> Predictions { get; init; } = [];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
