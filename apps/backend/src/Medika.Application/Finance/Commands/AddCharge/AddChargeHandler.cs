using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Commands.AddCharge;

public class AddChargeHandler(IChargeRepository charges, ICurrentUserService currentUser, IAuditService audit)
    : ICommandHandler<AddChargeCommand, string>
{
    public async Task<string> ExecuteAsync(AddChargeCommand cmd, CancellationToken ct)
    {
        var category = Enum.Parse<ChargeCategory>(cmd.Category, ignoreCase: true);
        var date = DateOnly.Parse(cmd.Date);

        var charge = Charge.Add(currentUser.UserId, category, cmd.Description, cmd.Amount, date, cmd.IsRecurring);
        await charges.AddAsync(charge, ct);
        await audit.LogAsync("AddCharge", "Charge", charge.Id.ToString(),
            after: new { charge.Category, charge.Amount }, ct: ct);

        return charge.Id.ToString();
    }
}
