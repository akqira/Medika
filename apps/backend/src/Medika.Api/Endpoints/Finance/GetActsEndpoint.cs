using FastEndpoints;
using Medika.Application.Finance.Queries.GetActs;

namespace Medika.Api.Endpoints.Finance;

public class GetActsEndpoint : EndpointWithoutRequest<ActsResult>
{
    public override void Configure()
    {
        Get("/api/acts");
        Permissions(PermissionConstants.Finance.ViewInvoices);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Response = await new GetActsQuery().ExecuteAsync(ct);
    }
}
