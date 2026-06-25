using Medika.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Medika.Infrastructure.Auth;

/// <summary>
/// Delivers the password-reset link by email via <see cref="IEmailService"/> (Brevo).
/// Builds the link from the raw token + App:BaseUrl, logs it (so the flow stays testable
/// locally / in CI where no provider is configured), then sends the email. When Brevo is
/// not configured, <see cref="IEmailService"/> simply skips the send — the flow is unaffected.
/// Replaces the earlier log-only <see cref="LoggingPasswordResetSender"/>.
/// </summary>
public class EmailPasswordResetSender(
    IConfiguration config,
    IEmailService email,
    ILogger<EmailPasswordResetSender> logger) : IPasswordResetSender
{
    public async Task SendResetLinkAsync(string emailAddress, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(emailAddress)}";

        // Logged so the flow is testable end-to-end even without a configured provider.
        logger.LogInformation("[PasswordReset] Reset link for {Email}: {Link}", emailAddress, link);

        const string subject = "Réinitialisation de votre mot de passe Medika";
        var html =
            $"""
            <div style="font-family:'DM Sans',Arial,sans-serif;max-width:480px;margin:0 auto;color:#1C2B3A;">
              <h2 style="color:#0E7C7B;">Réinitialisation du mot de passe</h2>
              <p>Vous avez demandé à réinitialiser votre mot de passe Medika.</p>
              <p>Cliquez sur le bouton ci-dessous pour choisir un nouveau mot de passe. Ce lien est valable 30 minutes.</p>
              <p style="margin:24px 0;">
                <a href="{link}" style="background:#0E7C7B;color:#fff;padding:12px 20px;border-radius:8px;text-decoration:none;display:inline-block;">
                  Réinitialiser mon mot de passe
                </a>
              </p>
              <p style="font-size:13px;color:#5b6b7a;">Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :<br>
                <a href="{link}">{link}</a>
              </p>
              <p style="font-size:13px;color:#5b6b7a;">Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet e-mail.</p>
            </div>
            """;

        await email.SendEmailAsync(emailAddress, subject, html, isHtml: true, ct);
    }
}
