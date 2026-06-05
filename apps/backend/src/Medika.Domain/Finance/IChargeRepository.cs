using Medika.Domain.Common;

namespace Medika.Domain.Finance;

public interface IChargeRepository : IRepository<Charge, ChargeId>
{
    Task<IReadOnlyList<Charge>> GetByPeriodAsync(string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<decimal> SumByPeriodAsync(string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
