using Medika.Domain.Medical;
using Medika.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence.Repositories;

public class ConsultationRepository(MongoContext ctx)
    : BaseRepository<Consultation, ConsultationId>(ctx.Consultations), IConsultationRepository
{
    public async Task<IReadOnlyList<Consultation>> GetByPatientAsync(string patientId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Consultation>.Filter.Eq(c => c.PatientId, patientId))
            .SortByDescending(c => c.Date).ToListAsync(ct);

    public async Task<Consultation?> GetByAppointmentAsync(string appointmentId, CancellationToken ct = default) =>
        await Collection.Find(Builders<Consultation>.Filter.Eq(c => c.AppointmentId, appointmentId))
            .FirstOrDefaultAsync(ct);
}
