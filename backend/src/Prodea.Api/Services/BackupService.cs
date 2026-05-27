using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Resend;

namespace Prodea.Api.Services;

public class BackupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<BackupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan InitialDelay  = TimeSpan.FromHours(1);
    private static readonly TimeSpan BackupInterval = TimeSpan.FromDays(3);

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
        var adminEmail = config["Backup:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            logger.LogWarning("Backup:AdminEmail no configurado, saltando backup");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();

        var predictions = await db.Predictions
            .AsNoTracking()
            .Select(p => new
            {
                p.UserId,
                p.MatchId,
                p.PredictedHomeScore,
                p.PredictedAwayScore,
                p.PointsEarned,
                UpdatedAt = p.UpdatedAt.ToString("o"),
            })
            .ToListAsync(ct);

        if (predictions.Count == 0)
        {
            logger.LogInformation("Backup: sin predicciones que respaldar");
            return;
        }

        var payload = new
        {
            generatedAt = DateTime.UtcNow.ToString("o"),
            count = predictions.Count,
            predictions,
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        var sizeKb = Encoding.UTF8.GetByteCount(json) / 1024.0;

        var from = config["Resend:From"] ?? "Prodeá <noreply@prodea.app>";
        var resend = scope.ServiceProvider.GetRequiredService<IResend>();

        var message = new EmailMessage
        {
            From = from,
            To = { adminEmail },
            Subject = $"[Prodeá] Backup predicciones — {DateTime.UtcNow:yyyy-MM-dd}",
            HtmlBody = $"""
                <div style="font-family: monospace; background:#0D0D0D; color:#fff; padding:24px; border-radius:8px; max-width:600px;">
                    <h2 style="color:#00FF87; margin:0 0 12px;">Backup predicciones Prodeá</h2>
                    <p style="color:#8A8A9A; margin:0 0 8px;">Fecha: <strong style="color:#fff">{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</strong></p>
                    <p style="color:#8A8A9A; margin:0 0 16px;">Predicciones: <strong style="color:#fff">{predictions.Count}</strong> &nbsp;|&nbsp; Tamaño JSON: <strong style="color:#fff">{sizeKb:F1} KB</strong></p>
                    <pre style="background:#1A1A2E; padding:16px; border-radius:6px; color:#8A8A9A; font-size:11px; overflow-x:auto; max-height:400px;">{json[..Math.Min(json.Length, 8000)]}{(json.Length > 8000 ? "\n... (truncado, ver adjunto)" : "")}</pre>
                    <p style="margin-top:16px; color:#3A3A4E; font-size:12px;">Este email es automático — generado por Prodeá.</p>
                </div>
                """,
            TextBody = json,
        };

        await resend.EmailSendAsync(message);
        logger.LogInformation("Backup enviado: {Count} predicciones ({KB:F1} KB) a {Email}", predictions.Count, sizeKb, adminEmail);
    }
}
