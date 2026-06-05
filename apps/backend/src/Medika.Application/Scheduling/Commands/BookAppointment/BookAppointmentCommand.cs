using FastEndpoints;

namespace Medika.Application.Scheduling.Commands.BookAppointment;

public record BookAppointmentCommand(
    string PatientId,
    string Date,        // "2026-06-05"
    string Time,        // "09:00"
    int DurationMinutes,
    string Reason,
    string Type         // "FollowUp" | "FirstVisit" | etc.
) : ICommand<string>;
