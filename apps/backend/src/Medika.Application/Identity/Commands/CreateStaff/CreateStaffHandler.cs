using FastEndpoints;
using Medika.Application.Authorization;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.CreateStaff;

public class CreateStaffHandler(
    IUserRepository users,
    ICurrentUserService currentUser,
    IAuditService audit,
    IPasswordHasher hasher) : ICommandHandler<CreateStaffCommand, string>
{
    public async Task<string> ExecuteAsync(CreateStaffCommand cmd, CancellationToken ct)
    {
        // New staff always join the creating doctor's cabinet — never a body/URL-supplied value.
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        if (await users.EmailExistsAsync(cmd.Email, ct))
            throw new InvalidOperationException($"Email {cmd.Email} is already registered.");

        // Unknown/forged permission strings are dropped; null falls back to the default set.
        var permissions = cmd.Permissions is null
            ? PermissionConstants.DefaultSecretary
            : PermissionConstants.Sanitize(cmd.Permissions).ToList();

        var hash = hasher.Hash(cmd.Password);
        var user = User.Create(
            cmd.Email, hash, cmd.FirstName, cmd.LastName,
            Role.Secretary, cabinetId: cabinetId, permissions: permissions);

        await users.AddAsync(user, ct);
        await audit.LogAsync("CreateStaff", "User", user.Id.ToString(),
            after: new { user.Email, Role = user.Role.ToString(), Permissions = permissions }, ct: ct);

        return user.Id.ToString();
    }
}
