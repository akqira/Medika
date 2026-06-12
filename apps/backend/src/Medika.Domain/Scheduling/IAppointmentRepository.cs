using Medika.Domain.Common;

namespace Medika.Domain.Scheduling;

public interface IAppointmentRepository : IRepository<Appointment, AppointmentId>
{
    Task<IReadOnlyList<Appointment>> GetByDateAsync(string cabinetId, string doctorId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<Appointment>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Appointment>> GetByWeekAsync(string cabinetId, string doctorId, DateOnly weekStart, CancellationToken ct = default);
    Task<bool> HasConflictAsync(string cabinetId, string doctorId, DateOnly date, TimeOnly time, int durationMinutes, string? excludeId = null, CancellationToken ct = default);
}
