using FastEndpoints;
using Medika.Application.Scheduling.Commands.NoShowAppointment;

namespace Medika.Api.Endpoints.Scheduling;

public class NoShowAppointmentEndpoint : EndpointWithoutRequest<AppointmentStatusResponse>
{
    public override void Configure()
    {
        Patch("/api/appointments/{id}/no-show");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id")!;

        try
        {
            var result = await new NoShowAppointmentCommand(id).ExecuteAsync(ct);
            await HttpContext.Response.SendAsync(new AppointmentStatusResponse(result.AppointmentId, result.Status), 200, null, ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (UnauthorizedAccessException)
        {
            await HttpContext.Response.SendForbiddenAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
        }
    }
}

public record AppointmentStatusResponse(string AppointmentId, string Status);
