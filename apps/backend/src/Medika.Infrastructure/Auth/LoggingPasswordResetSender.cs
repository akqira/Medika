using Medika.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Medika.Infrastructure.Auth;

/// <summary>
/// Placeholder delivery: builds the reset link and logs it instead of sending an
/// email. Swap for a real transactional-email implementation (Resend/SendGrid/SMTP)
/// once a provider account is configured — the interface and call-site stay the same.
/// </summary>
public class LoggingPasswordResetSender(
    IConfiguration config,
    ILogger<LoggingPasswordResetSender> logger) : IPasswordResetSender
{
    public Task SendResetLinkAsync(string email, string rawToken, string? displayName = null, CancellationToken ct = default)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(email)}";

        // NOT a real email yet — the link is logged so the flow is testable end-to-end.
        logger.LogInformation("[PasswordReset] Reset link for {Email}: {Link}", email, link);
        return Task.CompletedTask;
    }
}
