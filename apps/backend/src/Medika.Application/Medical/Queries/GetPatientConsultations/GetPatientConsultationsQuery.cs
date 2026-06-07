using FastEndpoints;

namespace Medika.Application.Medical.Queries.GetPatientConsultations;

public record GetPatientConsultationsQuery(
    string PatientId,
    int Page,
    int PageSize) : ICommand<ConsultationListResult>;

public record ConsultationListResult(
    string PatientId,
    long TotalCount,
    int Page,
    int PageSize,
    List<ConsultationSummary> Items);

public record ConsultationSummary(
    string ConsultationId,
    DateTime Date,
    string Reason,
    string? Diagnosis,
    decimal Tariff,
    bool IsFinalized,
    int PrescriptionCount,
    string? AppointmentId);

public record ConsultationDetail(
    string ConsultationId,
    string PatientId,
    string DoctorId,
    string? AppointmentId,
    DateTime Date,
    string Reason,
    string? ClinicalExam,
    string? Diagnosis,
    string? Notes,
    VitalSignsDetail? VitalSigns,
    List<PrescriptionLineDetail> Prescription,
    decimal Tariff,
    bool IsFinalized);

public record VitalSignsDetail(
    string? BloodPressure,
    int? PulseRate,
    decimal? Weight,
    decimal? Temperature,
    int? SpO2,
    decimal? Height);

public record PrescriptionLineDetail(
    string Medication,
    string Dosage,
    string? Duration,
    int Quantity,
    string? Frequency);
