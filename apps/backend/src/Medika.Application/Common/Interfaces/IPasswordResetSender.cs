namespace Medika.Application.Common.Interfaces;

/// <summary>
/// Delivers a password-reset link to the user. The implementation owns the
/// channel (email today) and how the link is built from the raw token.
/// </summary>
public interface IPasswordResetSender
{
    /// <param name="email">Recipient address (also encoded into the reset link).</param>
    /// <param name="rawToken">The single-use reset token.</param>
    /// <param name="displayName">
    /// The recipient's display name for a personalised greeting (e.g. "Dr. Kader Kebir").
    /// When null/blank the implementation falls back to a generic greeting.
    /// </param>
    Task SendResetLinkAsync(string email, string rawToken, string? displayName = null, CancellationToken ct = default);
}
