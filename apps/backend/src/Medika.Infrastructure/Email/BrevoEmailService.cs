using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Medika.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Medika.Infrastructure.Email;

/// <summary>
/// Brevo (ex-Sendinblue) transactional email provider.
/// Sends via the HTTP API: POST {BaseUrl}/smtp/email with an "api-key" header.
/// The typed <see cref="HttpClient"/> (BaseAddress + api-key header) is configured at
/// DI registration — see <c>DependencyInjection.AddEmail</c>.
///
/// When Brevo is not configured (no API key / sender — the local &amp; CI default),
/// sending is skipped with a warning instead of throwing, so flows that send mail
/// (e.g. password reset) keep working end-to-end without a real provider account.
/// </summary>
public class BrevoEmailService(
    HttpClient httpClient,
    BrevoSettings settings,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default)
        => SendEmailAsync(new[] { to }, subject, body, isHtml, ct);

    public async Task SendEmailAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(to);
        var recipients = to.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();

        if (recipients.Count == 0)
            throw new ArgumentException("At least one recipient email address is required", nameof(to));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject is required", nameof(subject));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Email body is required", nameof(body));

        // Not configured (local/CI default): don't send, don't crash the calling flow.
        if (!settings.IsConfigured)
        {
            logger.LogWarning(
                "Brevo is not configured (missing ApiKey/FromEmail) — skipping email \"{Subject}\" to {Recipients}.",
                subject, string.Join(", ", recipients));
            return;
        }

        var request = new BrevoSendEmailRequest
        {
            Sender = new BrevoContact(settings.FromName, settings.FromEmail),
            To = recipients.Select(addr => new BrevoContact(null, addr)).ToList(),
            Subject = subject,
            HtmlContent = isHtml ? body : null,
            TextContent = isHtml ? null : body,
        };

        logger.LogInformation("Sending email via Brevo to {RecipientCount} recipient(s).", recipients.Count);

        using var response = await httpClient.PostAsJsonAsync("smtp/email", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Brevo email send failed with status {StatusCode}. Response: {Response}",
                (int)response.StatusCode, error);

            throw new HttpRequestException(
                $"Brevo email send failed with status {(int)response.StatusCode}: {error}");
        }

        logger.LogInformation("Email sent successfully via Brevo to {RecipientCount} recipient(s).", recipients.Count);
    }

    // --- Brevo API payload (https://developers.brevo.com/reference/sendtransacemail) ---

    private sealed class BrevoSendEmailRequest
    {
        [JsonPropertyName("sender")]
        public BrevoContact Sender { get; init; } = null!;

        [JsonPropertyName("to")]
        public List<BrevoContact> To { get; init; } = new();

        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HtmlContent { get; init; }

        [JsonPropertyName("textContent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextContent { get; init; }
    }

    private sealed class BrevoContact(string? name, string email)
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; } = name;

        [JsonPropertyName("email")]
        public string Email { get; } = email;
    }
}
