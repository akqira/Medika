using FastEndpoints;
using Medika.Application.Finance.Commands.MarkInvoicePaid;

namespace Medika.Api.Endpoints.Finance;

public class MarkInvoicePaidEndpoint : Endpoint<MarkInvoicePaidCommand>
{
    public override void Configure()
    {
        Patch("/api/invoices/{InvoiceId}/pay");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(MarkInvoicePaidCommand req, CancellationToken ct)
    {
        await req.ExecuteAsync(ct);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
