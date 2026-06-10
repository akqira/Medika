using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Scheduling;

namespace Medika.Application.Scheduling.Commands.NoShowAppointment;

public class NoShowAppointmentHandler(
    IAppointmentRepository appointments,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<NoShowAppointmentCommand, NoShowAppointmentResult>
{
    public async Task<NoShowAppointmentResult> ExecuteAsync(NoShowAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await appointments.GetByIdAsync(AppointmentId.From(cmd.AppointmentId), ct);

        // Cabinet guard — cross-cabinet access is indistinguishable from not-found (no information leak).
        if (appointment is null ||
            (!string.IsNullOrEmpty(appointment.CabinetId) && appointment.CabinetId != currentUser.CabinetId))
            throw new KeyNotFoundException($"Appointment '{cmd.AppointmentId}' not found.");

        if (appointment.DoctorId != currentUser.UserId)
            throw new UnauthorizedAccessException("You are not authorised to mark this appointment as no-show.");

        appointment.MarkNoShow();

        await appointments.UpdateAsync(appointment, ct);
        await audit.LogAsync("NoShowAppointment", "Appointment", appointment.Id.ToString(),
            after: new { appointment.Status }, ct: ct);

        return new NoShowAppointmentResult(cmd.AppointmentId, appointment.Status.ToString());
    }
}
