using FastEndpoints;
using Medika.Application.Authorization;
using Medika.Application.Common.Interfaces;
using Medika.Application.Identity.Common;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Queries.GetCabinetUsers;

public class GetCabinetUsersHandler(IUserRepository users, ICurrentUserService currentUser)
    : ICommandHandler<GetCabinetUsersQuery, IReadOnlyList<CabinetUserDto>>
{
    public async Task<IReadOnlyList<CabinetUserDto>> ExecuteAsync(GetCabinetUsersQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var roster = await users.GetByCabinetAsync(cabinetId, ct);

        return roster
            // Expose the *effective* permission set so the UI shows a Doctor as full-admin
            // and a Secretary with exactly the set the doctor will edit.
            .Select(u => new CabinetUserDto(
                u.Id.ToString(), u.Email, u.FirstName, u.LastName,
                u.Role.ToString(), u.IsActive, PermissionResolver.Resolve(u), u.LastLoginAt))
            .ToList();
    }
}
