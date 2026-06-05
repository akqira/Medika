using FastEndpoints;
using Medika.Application.Patients.Commands.CreatePatient;

namespace Medika.Api.Endpoints.Patients;

public class CreatePatientEndpoint : Endpoint<CreatePatientCommand, CreatedPatientResponse>
{
    public override void Configure()
    {
        Post("/api/patients");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(CreatePatientCommand req, CancellationToken ct)
    {
        var validator = new CreatePatientValidator();
        var validation = await validator.ValidateAsync(req, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                AddError(error.PropertyName, error.ErrorMessage);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
            return;
        }

        var id = await req.ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new CreatedPatientResponse(id), 201, null, ct);
    }
}

public record CreatedPatientResponse(string PatientId);
