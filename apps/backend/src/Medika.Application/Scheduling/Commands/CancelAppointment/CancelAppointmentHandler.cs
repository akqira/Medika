using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Scheduling;

namespace Medika.Application.Scheduling.Commands.CancelAppointment;

public class CancelAppointmentHandler(
    IAppointmentRepository appointments,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<CancelAppointmentCommand, CancelAppointmentResult>
{
    public async Task<CancelAppointmentResult> ExecuteAsync(CancelAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await appointments.GetByIdAsync(AppointmentId.From(cmd.AppointmentId), ct);

        // Cabinet guard — cross-cabinet access is indistinguishable from not-found (no information leak).
        if (appointment is null ||
            (!string.IsNullOrEmpty(appointment.CabinetId) && appointment.CabinetId != currentUser.CabinetId))
            throw new KeyNotFoundException($"Appointment '{cmd.AppointmentId}' not found.");

        if (appointment.DoctorId != currentUser.UserId)
            throw new UnauthorizedAccessException("You are not authorised to cancel this appointment.");

        appointment.Cancel(cmd.Reason);

        await appointments.UpdateAsync(appointment, ct);
        await audit.LogAsync("CancelAppointment", "Appointment", appointment.Id.ToString(),
            after: new { appointment.Status, cmd.Reason }, ct: ct);

        return new CancelAppointmentResult(cmd.AppointmentId, appointment.Status.ToString(), cmd.Reason);
    }
}
