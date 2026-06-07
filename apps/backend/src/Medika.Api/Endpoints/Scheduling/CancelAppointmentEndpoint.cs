using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Scheduling;

namespace Medika.Api.Endpoints.Scheduling;

public class CancelAppointmentRequest
{
    public string? Reason { get; init; }
}

public class CancelAppointmentEndpoint : Endpoint<CancelAppointmentRequest, CancelAppointmentResponse>
{
    public IAppointmentRepository Appointments { get; set; } = null!;
    public ICurrentUserService CurrentUser { get; set; } = null!;

    public override void Configure()
    {
        Patch("/api/appointments/{id}/cancel");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(CancelAppointmentRequest req, CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var appointment = await Appointments.GetByIdAsync(AppointmentId.From(id), ct);

        if (appointment is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        if (appointment.DoctorId != CurrentUser.UserId)
        {
            await HttpContext.Response.SendForbiddenAsync(ct);
            return;
        }

        try
        {
            appointment.Cancel(req.Reason);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
            return;
        }

        await Appointments.UpdateAsync(appointment, ct);
        await HttpContext.Response.SendAsync(new CancelAppointmentResponse(id, appointment.Status.ToString(), req.Reason), 200, null, ct);
    }
}

public record CancelAppointmentResponse(string AppointmentId, string Status, string? Reason);
