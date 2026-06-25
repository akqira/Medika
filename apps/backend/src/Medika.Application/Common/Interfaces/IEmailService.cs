namespace Medika.Application.Common.Interfaces;

/// <summary>
/// Sends transactional emails. The implementation owns the provider (Brevo today);
/// callers stay provider-agnostic. Best-effort contract: implementations throw on a
/// hard failure so the caller can decide whether to surface or swallow it.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an email to a single recipient.</summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body (HTML or plain text, per <paramref name="isHtml"/>).</param>
    /// <param name="isHtml">Whether the body is HTML formatted (default: true).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default);

    /// <summary>Sends an email to multiple recipients.</summary>
    /// <param name="to">Recipient email addresses.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body (HTML or plain text, per <paramref name="isHtml"/>).</param>
    /// <param name="isHtml">Whether the body is HTML formatted (default: true).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendEmailAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default);
}
