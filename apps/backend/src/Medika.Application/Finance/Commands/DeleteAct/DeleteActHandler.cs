using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Commands.DeleteAct;

public class DeleteActHandler(
    IActRepository acts,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<DeleteActCommand>
{
    public async Task ExecuteAsync(DeleteActCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var id = ActId.From(cmd.Id);
        var act = await acts.GetByIdAsync(id, ct);
        // Cross-cabinet access is indistinguishable from not-found (no information leak).
        if (act is null ||
            (!string.IsNullOrEmpty(act.CabinetId) && act.CabinetId != cabinetId))
            throw new KeyNotFoundException($"Act '{cmd.Id}' not found.");

        await acts.DeleteAsync(id, ct);
        await audit.LogAsync("DeleteAct", "Act", cmd.Id, before: new { act.Name, act.Tariff }, ct: ct);
    }
}
