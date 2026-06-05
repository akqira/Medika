using Medika.Domain.Common;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository<TAggregate, TId>(
    IMongoCollection<TAggregate> collection)
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
{
    protected readonly IMongoCollection<TAggregate> Collection = collection;

    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        var filter = Builders<TAggregate>.Filter.Eq("_id", id!.ToString());
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(TAggregate aggregate, CancellationToken ct = default)
    {
        await Collection.InsertOneAsync(aggregate, cancellationToken: ct);
        aggregate.ClearEvents();
    }

    public async Task UpdateAsync(TAggregate aggregate, CancellationToken ct = default)
    {
        var filter = Builders<TAggregate>.Filter.Eq("_id", aggregate.Id!.ToString());
        await Collection.ReplaceOneAsync(filter, aggregate, cancellationToken: ct);
        aggregate.ClearEvents();
    }

    public async Task DeleteAsync(TId id, CancellationToken ct = default)
    {
        var filter = Builders<TAggregate>.Filter.Eq("_id", id!.ToString());
        await Collection.DeleteOneAsync(filter, ct);
    }
}
