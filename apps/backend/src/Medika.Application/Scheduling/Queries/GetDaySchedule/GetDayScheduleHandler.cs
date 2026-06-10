using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;

namespace Medika.Application.Scheduling.Queries.GetDaySchedule;

public class GetDayScheduleHandler(
    IAppointmentRepository appointments,
    IPatientRepository patients,
    ICurrentUserService currentUser) : ICommandHandler<GetDayScheduleQuery, IReadOnlyList<AppointmentSlot>>
{
    public async Task<IReadOnlyList<AppointmentSlot>> ExecuteAsync(GetDayScheduleQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var date = DateOnly.Parse(query.Date);
        var appts = await appointments.GetByDateAsync(cabinetId, currentUser.UserId, date, ct);

        var slots = new List<AppointmentSlot>();
        foreach (var a in appts.OrderBy(x => x.Time))
        {
            var patient = await patients.GetByIdAsync(PatientId.From(a.PatientId), ct);
            // Cabinet guard — never expose a patient from another cabinet.
            if (patient is not null && !string.IsNullOrEmpty(patient.CabinetId) && patient.CabinetId != cabinetId)
                patient = null;
            slots.Add(new AppointmentSlot(
                a.Id.ToString(), a.PatientId,
                patient is null ? "Unknown" : $"{patient.FirstName} {patient.LastName}",
                patient?.Phone ?? "",
                a.Time.ToString("HH:mm"), a.DurationMinutes,
                a.Reason, a.Status.ToString(), a.Type.ToString(),
                patient?.BloodGroup?.ToString(),
                patient?.Allergies.ToList() ?? []));
        }
        return slots;
    }
}
