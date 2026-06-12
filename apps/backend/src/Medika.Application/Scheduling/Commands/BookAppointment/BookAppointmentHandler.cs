using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Scheduling;

namespace Medika.Application.Scheduling.Commands.BookAppointment;

public class BookAppointmentHandler(
    IAppointmentRepository appointments,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<BookAppointmentCommand, string>
{
    public async Task<string> ExecuteAsync(BookAppointmentCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var date = DateOnly.Parse(cmd.Date);
        var time = TimeOnly.Parse(cmd.Time);
        var type = Enum.Parse<AppointmentType>(cmd.Type, ignoreCase: true);

        var hasConflict = await appointments.HasConflictAsync(
            cabinetId, currentUser.UserId, date, time, cmd.DurationMinutes, ct: ct);
        if (hasConflict)
            throw new InvalidOperationException("Time slot conflicts with an existing appointment.");

        var appt = Appointment.Book(
            cabinetId,
            cmd.PatientId, currentUser.UserId,
            date, time, cmd.DurationMinutes, cmd.Reason, type);

        await appointments.AddAsync(appt, ct);
        await audit.LogAsync("BookAppointment", "Appointment", appt.Id.ToString(),
            after: new { appt.PatientId, appt.Date, appt.Time }, ct: ct);

        return appt.Id.ToString();
    }
}
