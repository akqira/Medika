using FastEndpoints;

namespace Medika.Application.Patients.Queries.GetPatientById;

public record GetPatientByIdQuery(string Id) : ICommand<PatientDetail?>;

public record PatientDetail(
    string Id,
    string FirstName,
    string LastName,
    string? DateOfBirth,
    int Age,
    string Gender,
    string Phone,
    string? Email,
    string? Address,
    string? Nss,
    string? BloodGroup,
    string? Wilaya,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? InsuranceProvider,
    string? MutualInsurance,
    string? CurrentTreatment,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<string> MedicalHistory,
    string? LastVisitAt,
    string CreatedAt);
