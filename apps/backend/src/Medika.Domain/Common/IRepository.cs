namespace Medika.Domain.Common;

public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task AddAsync(TAggregate aggregate, CancellationToken ct = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken ct = default);
    Task DeleteAsync(TId id, CancellationToken ct = default);
}
