using FastEndpoints;

namespace Medika.Application.Scheduling.Commands.CancelAppointment;

public record CancelAppointmentCommand(
    string AppointmentId,
    string? Reason = null) : ICommand<CancelAppointmentResult>;

public record CancelAppointmentResult(string AppointmentId, string Status, string? Reason);
