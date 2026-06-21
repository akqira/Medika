using FastEndpoints;
using Medika.Application.Finance.Commands.DeleteCharge;

namespace Medika.Api.Endpoints.Finance;

public class DeleteChargeEndpoint : Endpoint<DeleteChargeCommand>
{
    public override void Configure()
    {
        Delete("/api/charges/{id}");
        Roles("Doctor");
    }

    public override async Task HandleAsync(DeleteChargeCommand req, CancellationToken ct)
    {
        await req.ExecuteAsync(ct);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
