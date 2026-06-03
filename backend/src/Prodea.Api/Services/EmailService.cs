using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Prodea.Api.Services;

public class EmailService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<EmailService> logger)
{
    private const string AdminEmail = "francoamato92@gmail.com";
    private readonly string _frontendUrl = config["Frontend:Url"] ?? "http://localhost:5173";

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        var apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey no configurado");
        var resetLink = $"{_frontendUrl}/reset-password?token={token}";

        var payload = new
        {
            from = "Prodea <noreply@prodea.app>",
            to = new[] { toEmail },
            subject = "Recuperá tu contraseña de Prodea",
            html = $"""
                <div style="font-family: sans-serif; max-width: 480px; margin: 0 auto; background: #0D0D0D; color: #fff; padding: 32px; border-radius: 12px;">
                    <h1 style="color: #00FF87; font-size: 28px; margin-bottom: 8px;">Prodea 🏆</h1>
                    <p style="color: #8A8A9A;">Recibiste este email porque pediste recuperar tu contraseña.</p>
                    <p>Hacé clic en el botón para crear una nueva contraseña. El link vence en <strong>1 hora</strong>.</p>
                    <a href="{resetLink}" style="display: inline-block; margin-top: 16px; padding: 14px 28px; background: #00FF87; color: #0D0D0D; font-weight: bold; border-radius: 8px; text-decoration: none;">
                        Recuperar contraseña
                    </a>
                    <p style="margin-top: 24px; color: #8A8A9A; font-size: 13px;">
                        Si no pediste recuperar tu contraseña, ignorá este email.
                    </p>
                </div>
                """
        };

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.resend.com/emails", content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend error {response.StatusCode}: {body}");
        }

        logger.LogInformation("Email de recuperación enviado a {Email}", toEmail);
    }

    public async Task SendContactMessageAsync(string fromUsername, string fromEmail, string message)
    {
        var apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey no configurado");

        var payload = new
        {
            from = "Prodea <noreply@prodea.app>",
            to = new[] { AdminEmail },
            subject = $"Mensaje de contacto — {fromUsername}",
            html = $"""
                <div style="font-family: sans-serif; max-width: 480px; margin: 0 auto; background: #0D0D0D; color: #fff; padding: 32px; border-radius: 12px;">
                    <h1 style="color: #00FF87; font-size: 24px; margin-bottom: 8px;">Nuevo mensaje de contacto</h1>
                    <p style="color: #8A8A9A; margin-bottom: 4px;">De: <strong style="color:#fff">{fromUsername}</strong> ({fromEmail})</p>
                    <div style="margin-top: 16px; padding: 16px; background: #1A1A2E; border-radius: 8px; color: #fff; white-space: pre-wrap;">{System.Net.WebUtility.HtmlEncode(message)}</div>
                </div>
                """
        };

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.resend.com/emails", content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend error {response.StatusCode}: {body}");
        }

        logger.LogInformation("Mensaje de contacto enviado de {Username}", fromUsername);
    }
}
