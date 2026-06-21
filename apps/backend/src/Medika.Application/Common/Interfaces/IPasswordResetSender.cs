namespace Medika.Application.Common.Interfaces;

/// <summary>
/// Delivers a password-reset link to the user. The implementation owns the
/// channel (email today) and how the link is built from the raw token.
/// </summary>
public interface IPasswordResetSender
{
    Task SendResetLinkAsync(string email, string rawToken, CancellationToken ct = default);
}
