using Medika.Domain.Common;

namespace Medika.Domain.Patients;

public interface IPatientRepository : IRepository<Patient, PatientId>
{
    Task<IReadOnlyList<Patient>> SearchAsync(string? term, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(string? term, CancellationToken ct = default);
    Task<IReadOnlyList<Patient>> GetRecentAsync(int count, CancellationToken ct = default);
}
