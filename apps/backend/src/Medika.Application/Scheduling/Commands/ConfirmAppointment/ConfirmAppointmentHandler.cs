using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Scheduling;

namespace Medika.Application.Scheduling.Commands.ConfirmAppointment;

public class ConfirmAppointmentHandler(
    IAppointmentRepository appointments,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<ConfirmAppointmentCommand, AppointmentStatusResult>
{
    public async Task<AppointmentStatusResult> ExecuteAsync(ConfirmAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await appointments.GetByIdAsync(AppointmentId.From(cmd.AppointmentId), ct);

        // Cabinet guard — cross-cabinet access is indistinguishable from not-found (no information leak).
        if (appointment is null ||
            (!string.IsNullOrEmpty(appointment.CabinetId) && appointment.CabinetId != currentUser.CabinetId))
            throw new KeyNotFoundException($"Appointment '{cmd.AppointmentId}' not found.");

        if (appointment.DoctorId != currentUser.UserId)
            throw new UnauthorizedAccessException("You are not authorised to confirm this appointment.");

        appointment.Confirm();

        await appointments.UpdateAsync(appointment, ct);
        await audit.LogAsync("ConfirmAppointment", "Appointment", appointment.Id.ToString(),
            after: new { appointment.Status }, ct: ct);

        return new AppointmentStatusResult(cmd.AppointmentId, appointment.Status.ToString());
    }
}
