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
    // Object-typed so the handler can return either full ConsultationSummary (Doctor)
    // or trimmed ConsultationMetadata (Receptionist) per ADR-002. System.Text.Json
    // serializes each element by its runtime type, so clinical fields are physically
    // ABSENT from the receptionist payload — not merely null.
    IReadOnlyList<object> Items);

public record ConsultationSummary(
    string ConsultationId,
    DateTime Date,
    string Reason,
    string? Diagnosis,
    decimal Tariff,
    bool IsFinalized,
    int PrescriptionCount,
    string? AppointmentId);

/// <summary>
/// Receptionist-safe projection (ADR-002): a visit happened and when, nothing clinical.
/// Deliberately carries NO Reason / Diagnosis / Tariff / PrescriptionCount — front-desk
/// staff must never see diagnoses. Tariff reaches receptionists via invoices, not here.
/// </summary>
public record ConsultationMetadata(
    string ConsultationId,
    DateTime Date,
    bool IsFinalized);

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
    string? Frequency);
