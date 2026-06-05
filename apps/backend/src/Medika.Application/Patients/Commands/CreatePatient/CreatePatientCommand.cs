using FastEndpoints;

namespace Medika.Application.Patients.Commands.CreatePatient;

public record CreatePatientCommand(
    string FirstName, string LastName,
    string DateOfBirth,     // ISO: "1975-03-12"
    string Gender,          // "M" | "F"
    string Phone,
    string? Email = null,
    string? Address = null,
    string? Nss = null,
    string? BloodGroup = null,
    List<string>? Allergies = null,
    List<string>? MedicalHistory = null) : ICommand<string>;
