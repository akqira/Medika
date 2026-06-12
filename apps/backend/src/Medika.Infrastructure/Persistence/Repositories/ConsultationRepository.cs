using Medika.Domain.Medical;
using Medika.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class ConsultationRepository(MongoContext ctx)
    : BaseRepository<Consultation, ConsultationId>(ctx.Consultations), IConsultationRepository
{
    public async Task<IReadOnlyList<Consultation>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Consultation>.Filter.And(
            Builders<Consultation>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Consultation>.Filter.Eq(c => c.PatientId, patientId)))
            .SortByDescending(c => c.Date).ToListAsync(ct);

    public async Task<Consultation?> GetByAppointmentAsync(string cabinetId, string appointmentId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Consultation>.Filter.And(
            Builders<Consultation>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Consultation>.Filter.Eq(c => c.AppointmentId, appointmentId)))
            .FirstOrDefaultAsync(ct);

    public async Task<Consultation?> GetDraftAsync(string cabinetId, string patientId, string doctorId, CancellationToken ct = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var filter = Builders<Consultation>.Filter.And(
            Builders<Consultation>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Consultation>.Filter.Eq(c => c.PatientId, patientId),
            Builders<Consultation>.Filter.Eq(c => c.DoctorId, doctorId),
            Builders<Consultation>.Filter.Eq(c => c.IsFinalized, false),
            Builders<Consultation>.Filter.Gte(c => c.Date, todayStart),
            Builders<Consultation>.Filter.Lt(c => c.Date, todayEnd));
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<(List<Consultation> Items, long Total)> GetByPatientPagedAsync(
        string cabinetId, string patientId, int page, int pageSize, CancellationToken ct = default)
    {
        var filter = Builders<Consultation>.Filter.And(
            Builders<Consultation>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Consultation>.Filter.Eq(c => c.PatientId, patientId));
        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection.Find(filter)
            .SortByDescending(c => c.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<Consultation?> GetByIdStringAsync(string cabinetId, string consultationId, CancellationToken ct = default)
    {
        var filter = Builders<Consultation>.Filter.And(
            Builders<Consultation>.Filter.Eq(c => c.CabinetId, cabinetId),
            Builders<Consultation>.Filter.Eq("_id", consultationId));
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }
}
