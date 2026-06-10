using Medika.Domain.Common;

namespace Medika.Domain.Medical;

public interface IConsultationRepository : IRepository<Consultation, ConsultationId>
{
    Task<IReadOnlyList<Consultation>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default);
    Task<Consultation?> GetByAppointmentAsync(string cabinetId, string appointmentId, CancellationToken ct = default);

    /// <summary>Returns the single non-finalized consultation for a patient+doctor created today, or null.</summary>
    Task<Consultation?> GetDraftAsync(string cabinetId, string patientId, string doctorId, CancellationToken ct = default);

    /// <summary>Returns a page of consultations for a patient, ordered by date descending.</summary>
    Task<(List<Consultation> Items, long Total)> GetByPatientPagedAsync(
        string cabinetId, string patientId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Returns a single consultation by its string ID.</summary>
    Task<Consultation?> GetByIdStringAsync(string cabinetId, string consultationId, CancellationToken ct = default);
}
