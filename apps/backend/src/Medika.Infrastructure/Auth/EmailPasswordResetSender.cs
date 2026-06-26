using System.Net;
using Medika.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Medika.Infrastructure.Auth;

/// <summary>
/// Delivers the password-reset link by email via <see cref="IEmailService"/> (Brevo).
/// Builds the link from the raw token + App:BaseUrl, logs it (so the flow stays testable
/// locally / in CI where no provider is configured), then sends a branded HTML email.
/// When Brevo is not configured, <see cref="IEmailService"/> simply skips the send — the
/// flow is unaffected. Replaces the earlier log-only <see cref="LoggingPasswordResetSender"/>.
/// </summary>
/// <remarks>
/// The HTML is table-based with inline styles so it renders consistently across email
/// clients (Gmail, Outlook, Apple Mail). Brand icons are hosted PNGs served from the
/// frontend's <c>static/email/</c> folder at <c>{App:BaseUrl}/email/*.png</c> — email
/// clients strip SVG and CSS-drawn shapes, so raster assets are the reliable choice.
/// Template matches the design in issue #76.
/// </remarks>
public class EmailPasswordResetSender(
    IConfiguration config,
    IEmailService email,
    ILogger<EmailPasswordResetSender> logger) : IPasswordResetSender
{
    // Brand tokens (mirrors apps/frontend/src/app.css).
    private const string Font = "'DM Sans',-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif";
    private const string Navy = "#1C2B3A";
    private const string Teal = "#0F766E";
    private const string Amber = "#D97706";

    public async Task SendResetLinkAsync(
        string emailAddress, string rawToken, string? displayName = null, CancellationToken ct = default)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        var assets = $"{baseUrl}/email";
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(emailAddress)}";

        // Logged so the flow is testable end-to-end even without a configured provider.
        logger.LogInformation("[PasswordReset] Reset link for {Email}: {Link}", emailAddress, link);

        // Encode the name (user-controlled) before interpolating it into HTML.
        var name = (displayName ?? string.Empty).Trim();
        var greeting = name.Length > 0
            ? $"Bonjour <strong style=\"color:{Navy}\">{WebUtility.HtmlEncode(name)}</strong>,"
            : "Bonjour,";
        var linkHtml = WebUtility.HtmlEncode(link);
        var year = DateTime.UtcNow.Year;

        const string subject = "Réinitialisation de votre mot de passe MediKa";
        var html =
            $"""
            <!DOCTYPE html>
            <html lang="fr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>{subject}</title>
            </head>
            <body style="margin:0;padding:0;background:#ECE8E1;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;">Réinitialisez votre mot de passe MediKa — ce lien expire dans 30 minutes.</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#ECE8E1;">
                <tr>
                  <td align="center" style="padding:32px 16px;">
                    <table role="presentation" width="560" cellpadding="0" cellspacing="0" border="0" style="width:560px;max-width:560px;background:#F6F3EE;border-radius:16px;overflow:hidden;font-family:{Font};">

                      <!-- Hero -->
                      <tr>
                        <td style="background:{Navy};padding:28px 36px 36px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td align="left" style="padding-bottom:28px;">
                                <img src="{assets}/mark.png" width="32" height="32" alt="MediKa" style="vertical-align:middle;border:0;display:inline-block;">
                                <span style="vertical-align:middle;color:#ffffff;font-size:19px;font-weight:700;letter-spacing:-0.3px;padding-left:10px;font-family:{Font};">MediKa</span>
                              </td>
                            </tr>
                            <tr>
                              <td align="center" style="padding-top:8px;">
                                <img src="{assets}/lock-hero.png" width="64" height="64" alt="" style="border:0;display:block;margin:0 auto 22px;">
                                <h1 style="margin:0 0 12px;color:#ffffff;font-size:24px;line-height:1.3;font-weight:700;letter-spacing:-0.4px;font-family:{Font};">Réinitialisation du mot de passe</h1>
                                <p style="margin:0 auto;max-width:340px;color:rgba(255,255,255,0.6);font-size:14px;line-height:1.6;font-family:{Font};">Vous avez demandé à réinitialiser le mot de passe associé à votre compte MediKa.</p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:32px 36px 8px;">
                          <p style="margin:0 0 18px;color:{Navy};font-size:15px;line-height:1.6;font-family:{Font};">{greeting}</p>
                          <p style="margin:0 0 26px;color:#4A5A6A;font-size:14.5px;line-height:1.7;font-family:{Font};">Nous avons reçu une demande de réinitialisation du mot de passe pour votre compte MediKa. Cliquez sur le bouton ci-dessous pour choisir un nouveau mot de passe sécurisé.</p>

                          <!-- CTA -->
                          <table role="presentation" align="center" cellpadding="0" cellspacing="0" border="0" style="margin:0 auto 28px;">
                            <tr>
                              <td align="center" bgcolor="{Teal}" style="border-radius:10px;">
                                <a href="{link}" target="_blank" style="display:inline-block;padding:14px 30px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:10px;font-family:{Font};">Réinitialiser mon mot de passe</a>
                              </td>
                            </tr>
                          </table>

                          <!-- Expiry info box -->
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#E6F6F1;border-radius:12px;margin-bottom:8px;">
                            <tr>
                              <td valign="top" style="padding:16px 16px 16px 18px;width:52px;">
                                <img src="{assets}/clock.png" width="36" height="36" alt="" style="border:0;display:block;">
                              </td>
                              <td valign="middle" style="padding:16px 18px 16px 0;">
                                <p style="margin:0 0 3px;color:{Navy};font-size:14px;font-weight:700;font-family:{Font};">Ce lien expire dans 30 minutes</p>
                                <p style="margin:0;color:#4A5A6A;font-size:13px;line-height:1.55;font-family:{Font};">Passé ce délai, vous devrez soumettre une nouvelle demande de réinitialisation.</p>
                              </td>
                            </tr>
                          </table>

                          <hr style="border:none;border-top:1px solid #E4DFD6;margin:26px 0;">

                          <!-- Security box -->
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#FDF4E7;border-left:3px solid {Amber};border-radius:8px;">
                            <tr>
                              <td style="padding:16px 18px;">
                                <p style="margin:0 0 6px;color:{Amber};font-size:14px;font-weight:700;font-family:{Font};">
                                  <img src="{assets}/warning.png" width="16" height="16" alt="" style="vertical-align:-2px;border:0;">
                                  &nbsp;Vous n'êtes pas à l'origine de cette demande&nbsp;?
                                </p>
                                <p style="margin:0;color:#7A6A52;font-size:13px;line-height:1.6;font-family:{Font};">Ignorez simplement cet e-mail. Votre mot de passe restera inchangé. Si vous constatez une activité suspecte sur votre compte, contactez-nous immédiatement à <span style="color:{Amber};font-weight:600;">securite@medika.dz</span>.</p>
                              </td>
                            </tr>
                          </table>

                          <hr style="border:none;border-top:1px solid #E4DFD6;margin:26px 0;">

                          <!-- Fallback link box -->
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#FFFFFF;border:1px solid #EAE5DC;border-radius:10px;">
                            <tr>
                              <td style="padding:16px 18px;">
                                <p style="margin:0 0 8px;color:#94A1AE;font-size:11px;font-weight:600;letter-spacing:0.6px;font-family:{Font};">LIEN DE SECOURS — SI LE BOUTON NE FONCTIONNE PAS</p>
                                <a href="{link}" target="_blank" style="color:{Teal};font-size:12.5px;font-family:'Courier New',Courier,monospace;word-break:break-all;text-decoration:none;">{linkHtml}</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:12px 0 0;text-align:center;color:#A6A096;font-size:12px;font-style:italic;font-family:{Font};">Copiez et collez ce lien dans votre navigateur</p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="background:{Navy};padding:26px 36px;text-align:center;">
                          <div style="margin-bottom:14px;">
                            <img src="{assets}/mark.png" width="24" height="24" alt="MediKa" style="vertical-align:middle;border:0;display:inline-block;">
                            <span style="vertical-align:middle;color:#ffffff;font-size:15px;font-weight:700;padding-left:8px;font-family:{Font};">MediKa</span>
                          </div>
                          <p style="margin:0 0 12px;font-size:12.5px;font-family:{Font};">
                            <a href="#" style="color:rgba(255,255,255,0.55);text-decoration:none;">Conditions d'utilisation</a>
                            <span style="color:rgba(255,255,255,0.25);">&nbsp;&nbsp;·&nbsp;&nbsp;</span>
                            <a href="#" style="color:rgba(255,255,255,0.55);text-decoration:none;">Politique de confidentialité</a>
                            <span style="color:rgba(255,255,255,0.25);">&nbsp;&nbsp;·&nbsp;&nbsp;</span>
                            <a href="#" style="color:rgba(255,255,255,0.55);text-decoration:none;">Aide &amp; support</a>
                          </p>
                          <p style="margin:0;color:rgba(255,255,255,0.35);font-size:12px;font-family:{Font};">© {year} MediKa · Solution cloud 100% algérienne · Alger, Algérie</p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        await email.SendEmailAsync(emailAddress, subject, html, isHtml: true, ct);
    }
}
