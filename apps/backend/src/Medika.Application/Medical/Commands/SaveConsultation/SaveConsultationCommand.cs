using FastEndpoints;

namespace Medika.Application.Medical.Commands.SaveConsultation;

public record SaveConsultationCommand(
    string PatientId,
    string? AppointmentId,
    string Reason,
    string? ClinicalExam,
    string? Diagnosis,
    string? Notes,
    VitalSignsDto? VitalSigns,
    List<PrescriptionLineDto> Prescription,
    decimal Tariff,
    bool Finalize = false) : ICommand<string>;

public record VitalSignsDto(
    string? BloodPressure, int? PulseRate,
    decimal? Weight, decimal? Temperature, int? SpO2);

public record PrescriptionLineDto(
    string Medication, string Dosage,
    string? Duration, int Quantity);
