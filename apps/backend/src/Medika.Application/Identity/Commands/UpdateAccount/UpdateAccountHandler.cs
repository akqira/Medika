using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.UpdateAccount;

public class UpdateAccountHandler(
    IUserRepository users,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<UpdateAccountCommand>
{
    public async Task ExecuteAsync(UpdateAccountCommand cmd, CancellationToken ct)
    {
        // Check email uniqueness, excluding the current user's own email
        var normalised = cmd.Email.Trim().ToLowerInvariant();
        var user = await users.GetByIdAsync(UserId.From(currentUser.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        if (!string.Equals(user.Email, normalised, StringComparison.Ordinal))
        {
            var emailTaken = await users.EmailExistsAsync(normalised, ct);
            if (emailTaken)
                throw new InvalidOperationException("Email address is already in use.");
        }

        user.UpdateAccount(cmd.FirstName, cmd.LastName, normalised);
        await users.UpdateAsync(user, ct);
        await audit.LogAsync("UpdateAccount", "User", user.Id.ToString(),
            after: new { cmd.FirstName, cmd.LastName, Email = normalised }, ct: ct);
    }
}
