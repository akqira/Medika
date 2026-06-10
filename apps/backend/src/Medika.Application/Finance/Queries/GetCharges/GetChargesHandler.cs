using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Queries.GetCharges;

public class GetChargesHandler(
    IChargeRepository charges,
    ICurrentUserService currentUser) : ICommandHandler<GetChargesQuery, ChargesResult>
{
    public async Task<ChargesResult> ExecuteAsync(GetChargesQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var items = await charges.GetByDoctorAndMonthAsync(cabinetId, currentUser.UserId, query.Year, query.Month, ct);

        var chargeItems = items
            .Select(c => new ChargeItem(
                c.Id.ToString(),
                c.Category.ToString(),
                c.Description,
                c.Amount,
                c.Date.ToString("yyyy-MM-dd"),
                c.IsRecurring,
                c.CreatedAt))
            .ToList();

        return new ChargesResult(
            query.Year,
            query.Month,
            items.Sum(c => c.Amount),
            chargeItems);
    }
}
