using Medika.Domain.Patients;
using Medika.Infrastructure.Persistence;
using Medika.Infrastructure.Persistence.Repositories;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class PatientRepository(MongoContext ctx) : BaseRepository<Patient, PatientId>(ctx.Patients), IPatientRepository
{
    public async Task<IReadOnlyList<Patient>> SearchAsync(string cabinetId, string? term, int page, int pageSize, CancellationToken ct = default)
    {
        var filter = BuildSearchFilter(cabinetId, term);
        return await Collection.Find(filter)
            .SortBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize).Limit(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(string cabinetId, string? term, CancellationToken ct = default)
    {
        var filter = BuildSearchFilter(cabinetId, term);
        return (int)await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Patient>> GetRecentAsync(string cabinetId, int count, CancellationToken ct = default) =>
        await Collection.Find(Builders<Patient>.Filter.Eq(p => p.CabinetId, cabinetId))
            .SortByDescending(p => p.LastVisitAt)
            .Limit(count)
            .ToListAsync(ct);

    private static FilterDefinition<Patient> BuildSearchFilter(string cabinetId, string? term)
    {
        var cabinetFilter = Builders<Patient>.Filter.Eq(p => p.CabinetId, cabinetId);
        if (string.IsNullOrWhiteSpace(term)) return cabinetFilter;
        var regex = new MongoDB.Bson.BsonRegularExpression(term, "i");
        return Builders<Patient>.Filter.And(
            cabinetFilter,
            Builders<Patient>.Filter.Or(
                Builders<Patient>.Filter.Regex(p => p.LastName, regex),
                Builders<Patient>.Filter.Regex(p => p.FirstName, regex),
                Builders<Patient>.Filter.Regex(p => p.Phone, regex)));
    }
}
