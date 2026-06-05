using FastEndpoints;

namespace Medika.Application.Scheduling.Queries.GetDaySchedule;

public record GetDayScheduleQuery(string Date) : ICommand<IReadOnlyList<AppointmentSlot>>;

public record AppointmentSlot(
    string Id, string PatientId, string PatientName, string PatientPhone,
    string Time, int DurationMinutes, string Reason, string Status, string Type,
    string? PatientBloodGroup, List<string> PatientAllergies);
