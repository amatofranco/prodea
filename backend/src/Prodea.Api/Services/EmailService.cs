using Resend;

namespace Prodea.Api.Services;

public class EmailService(IResend resend, IConfiguration config)
{
    private readonly string _from = config["Resend:From"] ?? "Prodeá <noreply@prodea.app>";
    private readonly string _frontendUrl = config["Frontend:Url"] ?? "http://localhost:5173";

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        var resetLink = $"{_frontendUrl}/reset-password?token={token}";

        var message = new EmailMessage
        {
            From = _from,
            To = { toEmail },
            Subject = "Recuperá tu contraseña de Prodeá",
            HtmlBody = $"""
                <div style="font-family: sans-serif; max-width: 480px; margin: 0 auto; background: #0D0D0D; color: #fff; padding: 32px; border-radius: 12px;">
                    <h1 style="color: #00FF87; font-size: 28px; margin-bottom: 8px;">Prodeá 🏆</h1>
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

        await resend.EmailSendAsync(message);
    }
}
