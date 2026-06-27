using FastEndpoints;
using Medika.Application.Patients.Commands.UpdatePatient;

namespace Medika.Api.Endpoints.Patients;

public class UpdatePatientEndpoint : Endpoint<UpdatePatientCommand>
{
    public override void Configure()
    {
        Patch("/api/patients/{id}");
        Permissions(PermissionConstants.Patients.Edit);
    }

    public override async Task HandleAsync(UpdatePatientCommand req, CancellationToken ct)
    {
        await req.ExecuteAsync(ct);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
