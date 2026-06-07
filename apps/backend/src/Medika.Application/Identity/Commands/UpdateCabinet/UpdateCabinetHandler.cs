using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.UpdateCabinet;

public class UpdateCabinetHandler(
    IUserRepository users,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<UpdateCabinetCommand>
{
    public async Task ExecuteAsync(UpdateCabinetCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(UserId.From(currentUser.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        user.UpdateCabinet(
            cmd.CabinetName,
            cmd.Specialty,
            cmd.RppsNumber,
            cmd.CabinetAddress,
            cmd.CabinetCity,
            cmd.CabinetWilaya,
            cmd.CabinetPhone);

        await users.UpdateAsync(user, ct);
        await audit.LogAsync("UpdateCabinet", "User", user.Id.ToString(),
            after: new { cmd.CabinetName, cmd.Specialty }, ct: ct);
    }
}
