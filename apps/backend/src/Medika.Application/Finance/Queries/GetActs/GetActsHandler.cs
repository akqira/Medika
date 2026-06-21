using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Queries.GetActs;

public class GetActsHandler(
    IActRepository acts,
    ICurrentUserService currentUser) : ICommandHandler<GetActsQuery, ActsResult>
{
    public async Task<ActsResult> ExecuteAsync(GetActsQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var items = await acts.GetByCabinetAsync(cabinetId, ct);

        return new ActsResult(
            items.Select(a => new ActItem(a.Id.ToString(), a.Name, a.Tariff)).ToList());
    }
}
