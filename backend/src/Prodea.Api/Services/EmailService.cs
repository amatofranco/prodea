using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Prodea.Api.Services;

public class EmailService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<EmailService> logger)
{
    private const string ResendEndpoint = "https://api.resend.com/emails";

    private readonly string _adminEmail = config["Admin:Email"] ?? throw new InvalidOperationException("Admin:Email not configured");
    private readonly string _frontendUrl = config["Frontend:Url"] ?? "http://localhost:5173";

    public async Task SendPasswordResetAsync(string toEmail, string token, string lang = "es")
    {
        var apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey not configured");
        var resetLink = $"{_frontendUrl}/reset-password?token={token}";

        var (subject, html) = lang == "en"
            ? BuildResetEmailEn(resetLink)
            : BuildResetEmailEs(resetLink);

        var payload = new
        {
            from = "Prodea <noreply@prodea.app>",
            to = new[] { toEmail },
            subject,
            html,
        };

        await PostToResendAsync(apiKey, payload);
        logger.LogInformation("Password reset email sent to {Email} (lang={Lang})", toEmail, lang);
    }

    private static (string Subject, string Html) BuildResetEmailEs(string resetLink) => (
        "Recuperá tu contraseña de Prodea",
        $"""
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
    );

    private static (string Subject, string Html) BuildResetEmailEn(string resetLink) => (
        "Reset your Prodea password",
        $"""
        <div style="font-family: sans-serif; max-width: 480px; margin: 0 auto; background: #0D0D0D; color: #fff; padding: 32px; border-radius: 12px;">
            <h1 style="color: #00FF87; font-size: 28px; margin-bottom: 8px;">Prodea 🏆</h1>
            <p style="color: #8A8A9A;">You received this email because you requested a password reset.</p>
            <p>Click the button below to create a new password. The link expires in <strong>1 hour</strong>.</p>
            <a href="{resetLink}" style="display: inline-block; margin-top: 16px; padding: 14px 28px; background: #00FF87; color: #0D0D0D; font-weight: bold; border-radius: 8px; text-decoration: none;">
                Reset password
            </a>
            <p style="margin-top: 24px; color: #8A8A9A; font-size: 13px;">
                If you didn't request a password reset, you can ignore this email.
            </p>
        </div>
        """
    );

    public async Task SendContactMessageAsync(string fromUsername, string fromEmail, string message, string lang = "es")
    {
        var apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey not configured");

        var (subject, heading, fromLabel) = lang == "en"
            ? ($"Contact message — {fromUsername}", "New contact message", "From:")
            : ($"Mensaje de contacto — {fromUsername}", "Nuevo mensaje de contacto", "De:");

        var payload = new
        {
            from = "Prodea <noreply@prodea.app>",
            to = new[] { _adminEmail },
            subject,
            html = $"""
                <div style="font-family: sans-serif; max-width: 480px; margin: 0 auto; background: #0D0D0D; color: #fff; padding: 32px; border-radius: 12px;">
                    <h1 style="color: #00FF87; font-size: 24px; margin-bottom: 8px;">{heading}</h1>
                    <p style="color: #8A8A9A; margin-bottom: 4px;">{fromLabel} <strong style="color:#fff">{fromUsername}</strong> ({fromEmail})</p>
                    <div style="margin-top: 16px; padding: 16px; background: #1A1A2E; border-radius: 8px; color: #fff; white-space: pre-wrap;">{System.Net.WebUtility.HtmlEncode(message)}</div>
                </div>
                """
        };

        await PostToResendAsync(apiKey, payload);
        logger.LogInformation("Contact message sent from {Username} (lang={Lang})", fromUsername, lang);
    }

    private async Task PostToResendAsync(string apiKey, object payload)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ResendEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend error {response.StatusCode}: {body}");
        }
    }
}
