using FastEndpoints;

namespace Medika.Application.Scheduling.Commands.NoShowAppointment;

public record NoShowAppointmentCommand(string AppointmentId) : ICommand<NoShowAppointmentResult>;

public record NoShowAppointmentResult(string AppointmentId, string Status);
