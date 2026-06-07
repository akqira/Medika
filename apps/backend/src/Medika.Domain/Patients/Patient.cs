using Medika.Domain.Common;
using Medika.Domain.Patients.Events;

namespace Medika.Domain.Patients;

public sealed class Patient : AggregateRoot<PatientId>
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public string Gender { get; private set; } = null!;          // "M" | "F"
    public string Phone { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Nss { get; private set; }                     // Numéro sécurité sociale
    public BloodGroup? BloodGroup { get; private set; }
    public IReadOnlyList<string> Allergies => _allergies.AsReadOnly();
    public IReadOnlyList<string> MedicalHistory => _medicalHistory.AsReadOnly();
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    public string? DoctorId { get; private set; }
    public DateTime? LastVisitAt { get; private set; }

    public string? Wilaya { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public string? InsuranceProvider { get; private set; }       // "CNAS" | "CASNOS" | "Military" | "None"
    public string? MutualInsurance { get; private set; }
    public string? CurrentTreatment { get; private set; }

    private readonly List<string> _allergies = [];
    private readonly List<string> _medicalHistory = [];

    private Patient() { }

    public static Patient Create(
        string firstName, string lastName,
        DateOnly dateOfBirth, string gender,
        string phone, string? email = null,
        string? address = null, string? nss = null,
        string? bloodGroup = null, string? doctorId = null,
        string? wilaya = null,
        string? emergencyContactName = null, string? emergencyContactPhone = null,
        string? insuranceProvider = null, string? mutualInsurance = null,
        string? currentTreatment = null)
    {
        var patient = new Patient
        {
            Id = PatientId.New(),
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Phone = phone,
            Email = email,
            Address = address,
            Nss = nss,
            BloodGroup = bloodGroup is not null ? Patients.BloodGroup.From(bloodGroup) : null,
            DoctorId = doctorId,
            Wilaya = wilaya,
            EmergencyContactName = emergencyContactName,
            EmergencyContactPhone = emergencyContactPhone,
            InsuranceProvider = insuranceProvider,
            MutualInsurance = mutualInsurance,
            CurrentTreatment = currentTreatment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        patient.Raise(new PatientRegistered(patient.Id, patient.FirstName, patient.LastName));
        return patient;
    }

    public void Update(
        string firstName, string lastName, string phone,
        string? email, string? address, string? nss, string? bloodGroup)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        Address = address;
        Nss = nss;
        BloodGroup = bloodGroup is not null ? Patients.BloodGroup.From(bloodGroup) : null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAllergy(string allergy)
    {
        if (!_allergies.Contains(allergy, StringComparer.OrdinalIgnoreCase))
            _allergies.Add(allergy);
    }

    public void RemoveAllergy(string allergy) =>
        _allergies.RemoveAll(a => a.Equals(allergy, StringComparison.OrdinalIgnoreCase));

    public void AddMedicalHistory(string entry)
    {
        if (!_medicalHistory.Contains(entry, StringComparer.OrdinalIgnoreCase))
            _medicalHistory.Add(entry);
    }

    public void RecordVisit() => LastVisitAt = DateTime.UtcNow;

    public int Age => DateOfBirth.ToDateTime(TimeOnly.MinValue) is var dob
        ? (int)((DateTime.Today - dob).TotalDays / 365.25)
        : 0;
}
