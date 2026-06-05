using Medika.Domain.Common;

namespace Medika.Domain.Medical;

public interface IConsultationRepository : IRepository<Consultation, ConsultationId>
{
    Task<IReadOnlyList<Consultation>> GetByPatientAsync(string patientId, CancellationToken ct = default);
    Task<Consultation?> GetByAppointmentAsync(string appointmentId, CancellationToken ct = default);
}
