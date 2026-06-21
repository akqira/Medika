using FastEndpoints;
using Medika.Application.Finance.Commands.DeleteAct;

namespace Medika.Api.Endpoints.Finance;

public class DeleteActEndpoint : Endpoint<DeleteActCommand>
{
    public override void Configure()
    {
        Delete("/api/acts/{id}");
        Roles("Doctor");
    }

    public override async Task HandleAsync(DeleteActCommand req, CancellationToken ct)
    {
        await req.ExecuteAsync(ct);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
