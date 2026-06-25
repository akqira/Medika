using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.SetUserActive;

public class SetUserActiveHandler(
    IUserRepository users,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<SetUserActiveCommand>
{
    public async Task ExecuteAsync(SetUserActiveCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var user = await users.GetByIdAsync(UserId.From(cmd.UserId), ct);
        if (user is null || user.CabinetId != cabinetId)
            throw new KeyNotFoundException($"User '{cmd.UserId}' not found.");

        // The doctor/admin account cannot be deactivated (a cabinet must keep its admin),
        // and nobody may deactivate themselves.
        if (user.Role == Role.Doctor)
            throw new InvalidOperationException("Le compte du médecin (administrateur) ne peut pas être désactivé.");
        if (user.Id.ToString() == currentUser.UserId)
            throw new InvalidOperationException("Vous ne pouvez pas désactiver votre propre compte.");

        if (cmd.IsActive) user.Reactivate(); else user.Deactivate();
        await users.UpdateAsync(user, ct);

        await audit.LogAsync(cmd.IsActive ? "ReactivateUser" : "DeactivateUser", "User", user.Id.ToString(), ct: ct);
    }
}
