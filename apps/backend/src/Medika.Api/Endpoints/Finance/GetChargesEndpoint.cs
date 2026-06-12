using FastEndpoints;
using Medika.Application.Finance.Queries.GetCharges;

namespace Medika.Api.Endpoints.Finance;

public class GetChargesRequest
{
    [QueryParam] public int Year { get; init; }
    [QueryParam] public int Month { get; init; }
}

public class GetChargesEndpoint : Endpoint<GetChargesRequest, ChargesResult>
{
    public override void Configure()
    {
        Get("/api/charges");
        Roles("Doctor");
    }

    public override async Task<ChargesResult> ExecuteAsync(GetChargesRequest req, CancellationToken ct)
    {
        return await new GetChargesQuery(req.Year, req.Month).ExecuteAsync(ct);
    }
}
