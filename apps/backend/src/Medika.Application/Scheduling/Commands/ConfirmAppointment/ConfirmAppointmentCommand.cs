using FastEndpoints;

namespace Medika.Application.Scheduling.Commands.ConfirmAppointment;

public record ConfirmAppointmentCommand(string AppointmentId) : ICommand<AppointmentStatusResult>;

public record AppointmentStatusResult(string AppointmentId, string Status);
