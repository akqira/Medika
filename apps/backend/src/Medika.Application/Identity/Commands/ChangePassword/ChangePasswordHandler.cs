using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.ChangePassword;

public class ChangePasswordHandler(
    IUserRepository users,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    IAuditService audit) : ICommandHandler<ChangePasswordCommand>
{
    public async Task ExecuteAsync(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(UserId.From(currentUser.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        if (!passwordHasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        var newHash = passwordHasher.Hash(cmd.NewPassword);
        user.ChangePassword(newHash);
        await users.UpdateAsync(user, ct);
        await audit.LogAsync("ChangePassword", "User", user.Id.ToString(), ct: ct);
    }
}
