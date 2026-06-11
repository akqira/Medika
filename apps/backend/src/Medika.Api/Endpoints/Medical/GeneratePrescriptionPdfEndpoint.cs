using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Application.Medical.Pdf;
using Medika.Domain.Identity;
using Medika.Domain.Medical;
using Medika.Domain.Patients;

namespace Medika.Api.Endpoints.Medical;

public class GeneratePrescriptionPdfRequest
{
    public string Id { get; init; } = null!;
    public string ConsultationId { get; init; } = null!;
}

/// <summary>
/// Streams a printable ordonnance PDF for a consultation. Cabinet-scoped: the
/// consultation is loaded by the JWT's cabinetId and must belong to the patient
/// in the route — no cross-cabinet leak.
/// </summary>
public class GeneratePrescriptionPdfEndpoint : Endpoint<GeneratePrescriptionPdfRequest>
{
    public IConsultationRepository Consultations { get; set; } = null!;
    public IPatientRepository Patients { get; set; } = null!;
    public IUserRepository Users { get; set; } = null!;
    public IPrescriptionPdfGenerator PdfGenerator { get; set; } = null!;
    public ICurrentUserService CurrentUser { get; set; } = null!;

    public override void Configure()
    {
        Get("/api/patients/{id}/consultations/{consultationId}/ordonnance");
        Roles("Doctor");
    }

    public override async Task HandleAsync(GeneratePrescriptionPdfRequest req, CancellationToken ct)
    {
        var cabinetId = CurrentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
        {
            await HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        var consultation = await Consultations.GetByIdStringAsync(cabinetId, req.ConsultationId, ct);
        if (consultation is null || consultation.PatientId != req.Id || consultation.Prescription.Count == 0)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var patient = await Patients.GetByIdAsync(PatientId.From(req.Id), ct);
        if (patient is null || patient.CabinetId != cabinetId)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var doctor = await Users.GetByIdAsync(UserId.From(consultation.DoctorId), ct);

        var model = new PrescriptionPdfModel(
            new DoctorLetterhead(
                FullName: doctor is not null ? $"Dr {doctor.FirstName} {doctor.LastName}" : "Dr",
                Specialty: doctor?.Specialty,
                OrderNumber: doctor?.OrderNumber,
                CabinetName: doctor?.CabinetName,
                CabinetAddress: doctor?.CabinetAddress,
                CabinetCity: doctor?.CabinetCity,
                CabinetWilaya: doctor?.CabinetWilaya,
                CabinetPhone: doctor?.CabinetPhone),
            new PatientHeader($"{patient.FirstName} {patient.LastName}", patient.Age, patient.Gender),
            consultation.Date,
            consultation.Prescription
                .Select(p => new PrescriptionPdfLine(p.Medication, p.Dosage, p.Frequency, p.Duration, p.Quantity))
                .ToList());

        var bytes = PdfGenerator.Generate(model);

        var fileName = $"ordonnance-{Slug(patient.LastName)}-{consultation.Date:yyyyMMdd}.pdf";
        await HttpContext.Response.SendBytesAsync(bytes, fileName, "application/pdf", cancellation: ct);
    }

    private static string Slug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }
}
