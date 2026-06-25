namespace Medika.Infrastructure.Email;

/// <summary>
/// Configuration for the Brevo (ex-Sendinblue) transactional email API.
/// Bound from the "Brevo" config section (see appsettings.json).
/// </summary>
public class BrevoSettings
{
    public const string Section = "Brevo";

    /// <summary>
    /// Brevo API key (v3). Empty locally → email sending is skipped (see BrevoEmailService).
    /// Supplied in prod via the Brevo__ApiKey Azure App Service setting — never committed.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Verified sender address (From). Must be a verified Brevo sender or belong to an
    /// authenticated domain.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Sender display name (optional).</summary>
    public string? FromName { get; set; }

    /// <summary>Base URL of the Brevo API.</summary>
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3";

    /// <summary>True when both an API key and a sender address are configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(FromEmail);
}
