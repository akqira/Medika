using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Scheduling;

namespace Medika.Api.Endpoints.Scheduling;

public class NoShowAppointmentEndpoint : EndpointWithoutRequest<AppointmentStatusResponse>
{
    public IAppointmentRepository Appointments { get; set; } = null!;
    public ICurrentUserService CurrentUser { get; set; } = null!;

    public override void Configure()
    {
        Patch("/api/appointments/{id}/no-show");
        Roles("Doctor", "Receptionist");
    }

    public override async Task HandleAsync(CancellationToken ct)
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
            appointment.MarkNoShow();
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
            return;
        }

        await Appointments.UpdateAsync(appointment, ct);
        await HttpContext.Response.SendAsync(new AppointmentStatusResponse(id, appointment.Status.ToString()), 200, null, ct);
    }
}

public record AppointmentStatusResponse(string AppointmentId, string Status);
