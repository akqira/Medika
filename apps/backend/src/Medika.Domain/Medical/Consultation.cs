using Medika.Domain.Common;
using Medika.Domain.Medical.Events;

namespace Medika.Domain.Medical;

public sealed class Consultation : AggregateRoot<ConsultationId>
{
    public string CabinetId { get; private set; } = null!;
    public string PatientId { get; private set; } = null!;
    public string DoctorId { get; private set; } = null!;
    public string? AppointmentId { get; private set; }
    public DateTime Date { get; private init; }
    public string Reason { get; private set; } = null!;
    public string? ClinicalExam { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Notes { get; private set; }
    public VitalSigns? VitalSigns { get; private set; }
    public IReadOnlyList<PrescriptionLine> Prescription => _prescription.AsReadOnly();
    public decimal Tariff { get; private set; }
    public bool IsFinalized { get; private set; }

    // Not readonly: MongoDB's BsonClassMap needs to assign this backing field on deserialize.
    private List<PrescriptionLine> _prescription = [];

    private Consultation() { }

    public static Consultation Start(
        string cabinetId,
        string patientId, string doctorId,
        string reason, string? appointmentId = null)
    {
        return new Consultation
        {
            Id = ConsultationId.New(),
            CabinetId = cabinetId,
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = appointmentId,
            Reason = reason,
            Date = DateTime.UtcNow,
            IsFinalized = false,
        };
    }

    public void SetClinicalData(
        string? clinicalExam, string? diagnosis, string? notes,
        VitalSigns? vitalSigns)
    {
        ClinicalExam = clinicalExam;
        Diagnosis = diagnosis;
        Notes = notes;
        VitalSigns = vitalSigns;
    }

    public void AddPrescriptionLine(PrescriptionLine line) => _prescription.Add(line);

    public void ClearPrescription() => _prescription.Clear();

    public void SetTariff(decimal tariff)
    {
        if (tariff < 0) throw new ArgumentException("Tariff cannot be negative.");
        Tariff = tariff;
    }

    public void Complete()
    {
        if (IsFinalized) throw new InvalidOperationException("Consultation already finalized.");
        IsFinalized = true;
        Raise(new ConsultationCompleted(Id, PatientId, DoctorId, Tariff, Date));
    }
}
