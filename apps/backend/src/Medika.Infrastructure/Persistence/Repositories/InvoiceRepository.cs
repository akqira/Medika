using Medika.Domain.Finance;
using Medika.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(MongoContext ctx)
    : BaseRepository<Invoice, InvoiceId>(ctx.Invoices), IInvoiceRepository
{
    public async Task<IReadOnlyList<Invoice>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.CabinetId, cabinetId),
            Builders<Invoice>.Filter.Eq(i => i.PatientId, patientId)))
            .SortByDescending(i => i.IssuedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetByStatusAsync(string cabinetId, InvoiceStatus status, string doctorId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.CabinetId, cabinetId),
            Builders<Invoice>.Filter.Eq(i => i.DoctorId, doctorId),
            Builders<Invoice>.Filter.Eq(i => i.Status, status)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await Collection.Find(Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.CabinetId, cabinetId),
            Builders<Invoice>.Filter.Eq(i => i.DoctorId, doctorId),
            Builders<Invoice>.Filter.Gte(i => i.IssuedAt, fromDt),
            Builders<Invoice>.Filter.Lte(i => i.IssuedAt, toDt)))
            .ToListAsync(ct);
    }

    public async Task<decimal> SumByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var invoices = await GetByPeriodAsync(cabinetId, doctorId, from, to, ct);
        return invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount);
    }

    public async Task<string> GenerateNumberAsync(string cabinetId, CancellationToken ct = default)
    {
        // Invoice numbers are sequential PER CABINET (unique compound index: cabinetId + number).
        var year = DateTime.UtcNow.Year;
        var count = await Collection.CountDocumentsAsync(
            Builders<Invoice>.Filter.And(
                Builders<Invoice>.Filter.Eq(i => i.CabinetId, cabinetId),
                Builders<Invoice>.Filter.Regex(i => i.Number, new MongoDB.Bson.BsonRegularExpression($"^F-{year}-"))),
            cancellationToken: ct);
        return $"F-{year}-{(count + 1):D3}";
    }
}
