using FastEndpoints;
using Medika.Application.Authorization;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.SetUserPermissions;

public class SetUserPermissionsHandler(
    IUserRepository users,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<SetUserPermissionsCommand>
{
    public async Task ExecuteAsync(SetUserPermissionsCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var user = await users.GetByIdAsync(UserId.From(cmd.UserId), ct);
        // Cross-cabinet access is indistinguishable from not-found (no information leak).
        if (user is null || user.CabinetId != cabinetId)
            throw new KeyNotFoundException($"User '{cmd.UserId}' not found.");

        // A Doctor is the cabinet admin and implicitly holds every permission — it is not editable.
        if (user.Role == Role.Doctor)
            throw new InvalidOperationException("Les permissions d'un médecin (administrateur) ne peuvent pas être modifiées.");

        var before = user.Permissions.ToList();
        user.SetPermissions(PermissionConstants.Sanitize(cmd.Permissions));
        await users.UpdateAsync(user, ct);

        await audit.LogAsync("SetUserPermissions", "User", user.Id.ToString(),
            before: new { Permissions = before },
            after: new { Permissions = user.Permissions.ToList() }, ct: ct);
    }
}
