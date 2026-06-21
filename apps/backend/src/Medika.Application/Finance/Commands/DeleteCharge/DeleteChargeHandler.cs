using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Commands.DeleteCharge;

public class DeleteChargeHandler(
    IChargeRepository charges,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<DeleteChargeCommand>
{
    public async Task ExecuteAsync(DeleteChargeCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var id = ChargeId.From(cmd.Id);
        var charge = await charges.GetByIdAsync(id, ct);
        // Cross-cabinet access is indistinguishable from not-found (no information leak).
        if (charge is null ||
            (!string.IsNullOrEmpty(charge.CabinetId) && charge.CabinetId != cabinetId))
            throw new KeyNotFoundException($"Charge '{cmd.Id}' not found.");

        await charges.DeleteAsync(id, ct);
        await audit.LogAsync("DeleteCharge", "Charge", cmd.Id,
            before: new { charge.Category, charge.Amount }, ct: ct);
    }
}
