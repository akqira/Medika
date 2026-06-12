using Medika.Domain.Common;

namespace Medika.Domain.Patients;

public interface IPatientRepository : IRepository<Patient, PatientId>
{
    Task<IReadOnlyList<Patient>> SearchAsync(string cabinetId, string? term, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(string cabinetId, string? term, CancellationToken ct = default);
    Task<IReadOnlyList<Patient>> GetRecentAsync(string cabinetId, int count, CancellationToken ct = default);
}
