using FastEndpoints;
using Medika.Application.Finance.Queries.GetPatientInvoices;

namespace Medika.Api.Endpoints.Finance;

public class GetPatientInvoicesRequest
{
    public string Id { get; init; } = null!;
}

public class GetPatientInvoicesEndpoint : Endpoint<GetPatientInvoicesRequest, List<PatientInvoiceDto>>
{
    public override void Configure()
    {
        Get("/api/patients/{id}/invoices");
        Roles("Doctor", "Receptionist");
    }

    public override async Task<List<PatientInvoiceDto>> ExecuteAsync(GetPatientInvoicesRequest req, CancellationToken ct)
    {
        return await new GetPatientInvoicesQuery(req.Id).ExecuteAsync(ct);
    }
}
