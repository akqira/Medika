using Medika.Domain.Finance;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class ActRepository(MongoContext ctx)
    : BaseRepository<Act, ActId>(ctx.Acts), IActRepository
{
    public async Task<IReadOnlyList<Act>> GetByCabinetAsync(string cabinetId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Act>.Filter.Eq(a => a.CabinetId, cabinetId))
            .SortBy(a => a.Name).ToListAsync(ct);
}
