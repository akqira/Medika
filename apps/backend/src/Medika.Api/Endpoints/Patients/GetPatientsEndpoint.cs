using FastEndpoints;
using Medika.Application.Common.Models;
using Medika.Application.Patients.Queries.GetPatients;

namespace Medika.Api.Endpoints.Patients;

public class GetPatientsEndpoint : Endpoint<GetPatientsRequest, PagedResult<PatientSummary>>
{
    public override void Configure()
    {
        Get("/api/patients");
        Roles("Doctor", "Receptionist");
    }

    public override async Task<PagedResult<PatientSummary>> ExecuteAsync(GetPatientsRequest req, CancellationToken ct)
    {
        return await new GetPatientsQuery(req.Term, req.Page, req.PageSize).ExecuteAsync(ct);
    }
}

public record GetPatientsRequest(
    [property: QueryParam] string? Term,
    [property: QueryParam] int Page = 1,
    [property: QueryParam] int PageSize = 20);
