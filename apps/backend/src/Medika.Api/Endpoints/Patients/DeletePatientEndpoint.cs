using FastEndpoints;
using Medika.Application.Patients.Commands.DeletePatient;

namespace Medika.Api.Endpoints.Patients;

public class DeletePatientEndpoint : Endpoint<DeletePatientCommand>
{
    public override void Configure()
    {
        Delete("/api/patients/{id}");
        Roles("Doctor");
    }

    public override async Task HandleAsync(DeletePatientCommand req, CancellationToken ct)
    {
        await req.ExecuteAsync(ct);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
