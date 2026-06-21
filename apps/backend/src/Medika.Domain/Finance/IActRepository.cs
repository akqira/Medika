using Medika.Domain.Common;

namespace Medika.Domain.Finance;

public interface IActRepository : IRepository<Act, ActId>
{
    Task<IReadOnlyList<Act>> GetByCabinetAsync(string cabinetId, CancellationToken ct = default);
}
