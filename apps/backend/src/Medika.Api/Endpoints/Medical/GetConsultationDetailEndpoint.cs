using FastEndpoints;
using Medika.Application.Medical.Queries.GetPatientConsultations;
using Medika.Domain.Medical;

namespace Medika.Api.Endpoints.Medical;

public class GetConsultationDetailRequest
{
    public string Id { get; init; } = null!;
    public string ConsultationId { get; init; } = null!;
}

public class GetConsultationDetailEndpoint : Endpoint<GetConsultationDetailRequest, ConsultationDetail>
{
    public IConsultationRepository Consultations { get; set; } = null!;

    public override void Configure()
    {
        Get("/api/patients/{id}/consultations/{consultationId}");
        Roles("Doctor");
    }

    public override async Task HandleAsync(GetConsultationDetailRequest req, CancellationToken ct)
    {
        var consultation = await Consultations.GetByIdStringAsync(req.ConsultationId, ct);

        if (consultation is null || consultation.PatientId != req.Id)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var vitalSigns = consultation.VitalSigns is { } vs
            ? new VitalSignsDetail(vs.BloodPressure, vs.PulseRate, vs.Weight, vs.Temperature, vs.SpO2, vs.Height)
            : null;

        var prescription = consultation.Prescription
            .Select(p => new PrescriptionLineDetail(p.Medication, p.Dosage, p.Duration, p.Quantity, p.Frequency))
            .ToList();

        var detail = new ConsultationDetail(
            consultation.Id.ToString(),
            consultation.PatientId,
            consultation.DoctorId,
            consultation.AppointmentId,
            consultation.Date,
            consultation.Reason,
            consultation.ClinicalExam,
            consultation.Diagnosis,
            consultation.Notes,
            vitalSigns,
            prescription,
            consultation.Tariff,
            consultation.IsFinalized);

        await HttpContext.Response.SendAsync(detail, 200, null, ct);
    }
}
