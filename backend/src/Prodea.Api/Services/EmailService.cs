using System.Net;
using System.Net.Mail;

namespace Prodea.Api.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private readonly string _frontendUrl = config["Frontend:Url"] ?? "http://localhost:5173";

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        var host = config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host no configurado");
        var port = int.Parse(config["Smtp:Port"] ?? "587");
        var user = config["Smtp:User"] ?? throw new InvalidOperationException("Smtp:User no configurado");
        var pass = config["Smtp:Pass"] ?? throw new InvalidOperationException("Smtp:Pass no configurado");

        var resetLink = $"{_frontendUrl}/reset-password?token={token}";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true,
            Timeout = 10_000,
        };

        var mail = new MailMessage
        {
            From = new MailAddress(user, "Prodea"),
            Subject = "Recuperá tu contraseña de Prodea",
            IsBodyHtml = true,
            Body = $"""
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
        mail.To.Add(toEmail);

        await client.SendMailAsync(mail);
        logger.LogInformation("Email de recuperación enviado a {Email}", toEmail);
    }
}
