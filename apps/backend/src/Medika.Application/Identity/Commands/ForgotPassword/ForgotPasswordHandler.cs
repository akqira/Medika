using System.Security.Cryptography;
using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.ForgotPassword;

public class ForgotPasswordHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IPasswordResetSender sender) : ICommandHandler<ForgotPasswordCommand>
{
    // Reset links are valid for 30 minutes.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public async Task ExecuteAsync(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);

        // No account enumeration: if there's no (active) user we silently do nothing.
        // The endpoint always returns the same generic success either way.
        if (user is null || !user.IsActive)
            return;

        // Opaque, URL-safe single-use token. Only its hash is stored.
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.SetPasswordResetToken(hasher.Hash(rawToken), DateTime.UtcNow.Add(TokenLifetime));
        await users.UpdateAsync(user, ct);

        await sender.SendResetLinkAsync(email, rawToken, ct);
    }
}
