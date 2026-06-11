namespace Medika.Application.Medical.Pdf;

/// <summary>
/// Everything the PDF generator needs to render a printable ordonnance.
/// Assembled by the endpoint from the consultation, patient and prescribing doctor.
/// </summary>
public sealed record PrescriptionPdfModel(
    DoctorLetterhead Doctor,
    PatientHeader Patient,
    DateTime Date,
    IReadOnlyList<PrescriptionPdfLine> Lines);

/// <summary>Cabinet letterhead — sourced from the doctor's <c>User</c> profile fields.</summary>
public sealed record DoctorLetterhead(
    string FullName,
    string? Specialty,
    string? OrderNumber,
    string? CabinetName,
    string? CabinetAddress,
    string? CabinetCity,
    string? CabinetWilaya,
    string? CabinetPhone);

public sealed record PatientHeader(string FullName, int Age, string Gender);

public sealed record PrescriptionPdfLine(
    string Medication,
    string Dosage,
    string? Frequency,
    string? Duration,
    int Quantity);
