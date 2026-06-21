using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.ResetPassword;

public class ResetPasswordHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IAuditService audit) : ICommandHandler<ResetPasswordCommand>
{
    // Single generic message for every failure mode so a bad link can't be told
    // apart from an expired one or an unknown email (no enumeration).
    private const string InvalidLink = "Lien invalide ou expiré. Veuillez refaire une demande.";

    public async Task ExecuteAsync(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);

        if (user is null
            || !user.HasValidPasswordResetToken(DateTime.UtcNow)
            || !hasher.Verify(cmd.Token, user.PasswordResetTokenHash!))
            throw new ArgumentException(InvalidLink);

        user.ResetPassword(hasher.Hash(cmd.NewPassword));
        await users.UpdateAsync(user, ct);
        await audit.LogAsync("ResetPassword", "User", user.Id.ToString(), ct: ct);
    }
}
