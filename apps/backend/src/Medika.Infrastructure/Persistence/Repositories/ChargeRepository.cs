using Medika.Domain.Finance;
using Medika.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class ChargeRepository(MongoContext ctx)
    : BaseRepository<Charge, ChargeId>(ctx.Charges), IChargeRepository
{
    public async Task<IReadOnlyList<Charge>> GetByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await Collection.Find(Builders<Charge>.Filter.And(
            Builders<Charge>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Charge>.Filter.Eq(c => c.DoctorId, doctorId),
            Builders<Charge>.Filter.Gte(c => c.Date, from),
            Builders<Charge>.Filter.Lte(c => c.Date, to)))
            .SortByDescending(c => c.Date).ToListAsync(ct);

    public async Task<decimal> SumByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var charges = await GetByPeriodAsync(cabinetId, doctorId, from, to, ct);
        return charges.Sum(c => c.Amount);
    }

    public async Task<IReadOnlyList<Charge>> GetByDoctorAndMonthAsync(string cabinetId, string doctorId, int year, int month, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        return await Collection.Find(Builders<Charge>.Filter.And(
            Builders<Charge>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Charge>.Filter.Eq(c => c.DoctorId, doctorId),
            Builders<Charge>.Filter.Gte(c => c.Date, from),
            Builders<Charge>.Filter.Lte(c => c.Date, to)))
            .SortBy(c => c.Date).ToListAsync(ct);
    }
}
