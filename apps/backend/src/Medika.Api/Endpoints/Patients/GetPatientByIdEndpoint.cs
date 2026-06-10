using FastEndpoints;
using Medika.Application.Patients.Queries.GetPatientById;

namespace Medika.Api.Endpoints.Patients;

public class GetPatientByIdRequest
{
    public string Id { get; init; } = null!;
}

public class GetPatientByIdEndpoint : Endpoint<GetPatientByIdRequest, PatientDetail>
{
    public override void Configure()
    {
        Get("/api/patients/{id}");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(GetPatientByIdRequest req, CancellationToken ct)
    {
        var detail = await new GetPatientByIdQuery(req.Id).ExecuteAsync(ct);

        if (detail is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(detail, 200, null, ct);
    }
}
