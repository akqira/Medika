using Medika.Domain.Common;

namespace Medika.Domain.Finance;

public interface IChargeRepository : IRepository<Charge, ChargeId>
{
    Task<IReadOnlyList<Charge>> GetByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<decimal> SumByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<Charge>> GetByDoctorAndMonthAsync(string cabinetId, string doctorId, int year, int month, CancellationToken ct = default);
}
