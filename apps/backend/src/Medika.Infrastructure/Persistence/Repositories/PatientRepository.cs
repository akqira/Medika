using Medika.Domain.Patients;
using Medika.Infrastructure.Persistence;
using Medika.Infrastructure.Persistence.Repositories;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class PatientRepository(MongoContext ctx) : BaseRepository<Patient, PatientId>(ctx.Patients), IPatientRepository
{
    public async Task<IReadOnlyList<Patient>> SearchAsync(string? term, int page, int pageSize, CancellationToken ct = default)
    {
        var filter = BuildSearchFilter(term);
        return await Collection.Find(filter)
            .SortBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize).Limit(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(string? term, CancellationToken ct = default)
    {
        var filter = BuildSearchFilter(term);
        return (int)await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Patient>> GetRecentAsync(int count, CancellationToken ct = default) =>
        await Collection.Find(Builders<Patient>.Filter.Empty)
            .SortByDescending(p => p.LastVisitAt)
            .Limit(count)
            .ToListAsync(ct);

    private static FilterDefinition<Patient> BuildSearchFilter(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return Builders<Patient>.Filter.Empty;
        var regex = new MongoDB.Bson.BsonRegularExpression(term, "i");
        return Builders<Patient>.Filter.Or(
            Builders<Patient>.Filter.Regex(p => p.LastName, regex),
            Builders<Patient>.Filter.Regex(p => p.FirstName, regex),
            Builders<Patient>.Filter.Regex(p => p.Phone, regex));
    }
}
