using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Commands.AddAct;

public class AddActHandler(
    IActRepository acts,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<AddActCommand, string>
{
    public async Task<string> ExecuteAsync(AddActCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var act = Act.Create(cabinetId, cmd.Name, cmd.Tariff);
        await acts.AddAsync(act, ct);
        await audit.LogAsync("AddAct", "Act", act.Id.ToString(),
            after: new { act.Name, act.Tariff }, ct: ct);

        return act.Id.ToString();
    }
}
