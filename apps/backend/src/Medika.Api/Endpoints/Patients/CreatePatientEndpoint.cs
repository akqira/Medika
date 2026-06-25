using FastEndpoints;
using Medika.Application.Patients.Commands.CreatePatient;

namespace Medika.Api.Endpoints.Patients;

public class CreatePatientEndpoint : Endpoint<CreatePatientCommand, CreatedPatientResponse>
{
    public override void Configure()
    {
        Post("/api/patients");
        Permissions(PermissionConstants.Patients.Create);
    }

    public override async Task HandleAsync(CreatePatientCommand req, CancellationToken ct)
    {
        var id = await req.ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new CreatedPatientResponse(id), 201, null, ct);
    }
}

public record CreatedPatientResponse(string PatientId);
