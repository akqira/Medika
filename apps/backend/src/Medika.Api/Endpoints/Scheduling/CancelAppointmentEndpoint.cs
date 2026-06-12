using FastEndpoints;
using Medika.Application.Scheduling.Commands.CancelAppointment;

namespace Medika.Api.Endpoints.Scheduling;

public class CancelAppointmentRequest
{
    public string? Reason { get; init; }
}

public class CancelAppointmentEndpoint : Endpoint<CancelAppointmentRequest, CancelAppointmentResponse>
{
    public override void Configure()
    {
        Patch("/api/appointments/{id}/cancel");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(CancelAppointmentRequest req, CancellationToken ct)
    {
        var id = Route<string>("id")!;

        try
        {
            var result = await new CancelAppointmentCommand(id, req.Reason).ExecuteAsync(ct);
            await HttpContext.Response.SendAsync(new CancelAppointmentResponse(result.AppointmentId, result.Status, result.Reason), 200, null, ct);
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

public record CancelAppointmentResponse(string AppointmentId, string Status, string? Reason);
