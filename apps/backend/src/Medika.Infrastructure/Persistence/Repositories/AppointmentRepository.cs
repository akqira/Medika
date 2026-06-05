using Medika.Domain.Scheduling;
using Medika.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class AppointmentRepository(MongoContext ctx)
    : BaseRepository<Appointment, AppointmentId>(ctx.Appointments), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(string doctorId, DateOnly date, CancellationToken ct = default) =>
        await Collection.Find(Builders<Appointment>.Filter.And(
            Builders<Appointment>.Filter.Eq(a => a.DoctorId, doctorId),
            Builders<Appointment>.Filter.Eq(a => a.Date, date)))
            .SortBy(a => a.Time).ToListAsync(ct);

    public async Task<IReadOnlyList<Appointment>> GetByPatientAsync(string patientId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Appointment>.Filter.Eq(a => a.PatientId, patientId))
            .SortByDescending(a => a.Date).ThenByDescending(a => a.Time)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Appointment>> GetByWeekAsync(string doctorId, DateOnly weekStart, CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(6);
        return await Collection.Find(Builders<Appointment>.Filter.And(
            Builders<Appointment>.Filter.Eq(a => a.DoctorId, doctorId),
            Builders<Appointment>.Filter.Gte(a => a.Date, weekStart),
            Builders<Appointment>.Filter.Lte(a => a.Date, weekEnd)))
            .SortBy(a => a.Date).ThenBy(a => a.Time)
            .ToListAsync(ct);
    }

    public async Task<bool> HasConflictAsync(
        string doctorId, DateOnly date, TimeOnly time, int durationMinutes,
        string? excludeId = null, CancellationToken ct = default)
    {
        var endTime = time.AddMinutes(durationMinutes);
        var filter = Builders<Appointment>.Filter.And(
            Builders<Appointment>.Filter.Eq(a => a.DoctorId, doctorId),
            Builders<Appointment>.Filter.Eq(a => a.Date, date),
            Builders<Appointment>.Filter.Nin(a => a.Status,
                [AppointmentStatus.Cancelled, AppointmentStatus.NoShow]),
            Builders<Appointment>.Filter.Lt(a => a.Time, endTime),
            Builders<Appointment>.Filter.Gt(a => a.Time, time.AddMinutes(-durationMinutes)));

        if (excludeId is not null)
            filter &= Builders<Appointment>.Filter.Ne("_id", excludeId);

        return await Collection.CountDocumentsAsync(filter, cancellationToken: ct) > 0;
    }
}
