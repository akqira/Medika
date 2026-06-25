using Medika.Domain.Identity;
using Medika.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class UserRepository(MongoContext ctx)
    : BaseRepository<User, UserId>(ctx.Users), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Collection.Find(Builders<User>.Filter.Eq(u => u.Email, email.ToLowerInvariant()))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await Collection.CountDocumentsAsync(
            Builders<User>.Filter.Eq(u => u.Email, email.ToLowerInvariant()),
            cancellationToken: ct) > 0;

    public async Task<IReadOnlyList<User>> GetByCabinetAsync(string cabinetId, CancellationToken ct = default) =>
        await Collection
            // cabinetId is ALWAYS the first filter clause (multi-tenancy rule).
            .Find(Builders<User>.Filter.Eq(u => u.CabinetId, cabinetId))
            .SortBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);
}
