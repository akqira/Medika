using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;
using Microsoft.Extensions.Logging;

namespace Medika.Application.Medical.Commands.SaveConsultation;

public class SaveConsultationHandler(
    IConsultationRepository consultations,
    IAppointmentRepository appointments,
    IInvoiceRepository invoices,
    IPatientRepository patients,
    ICurrentUserService currentUser,
    IAuditService audit,
    ILogger<SaveConsultationHandler> logger) : ICommandHandler<SaveConsultationCommand, string>
{
    public async Task<string> ExecuteAsync(SaveConsultationCommand cmd, CancellationToken ct)
    {
        // Upsert semantics: when not finalizing, look for an existing draft for same patient+doctor today
        Consultation? existing = null;
        if (!cmd.Finalize)
            existing = await consultations.GetDraftAsync(cmd.PatientId, currentUser.UserId, ct);

        Consultation consultation;
        bool isNew;

        if (existing is not null)
        {
            consultation = existing;
            isNew = false;
        }
        else
        {
            consultation = Consultation.Start(
                cmd.PatientId, currentUser.UserId,
                cmd.Reason, cmd.AppointmentId);
            isNew = true;
        }

        var vitalSigns = cmd.VitalSigns is { } vs
            ? new VitalSigns(vs.BloodPressure, vs.PulseRate, vs.Weight, vs.Temperature, vs.SpO2, vs.Height)
            : null;

        consultation.SetClinicalData(cmd.ClinicalExam, cmd.Diagnosis, cmd.Notes, vitalSigns);

        consultation.ClearPrescription();
        foreach (var line in cmd.Prescription)
            consultation.AddPrescriptionLine(
                new PrescriptionLine(line.Medication, line.Dosage, line.Duration, line.Quantity, line.Frequency));

        consultation.SetTariff(cmd.Tariff);

        if (cmd.Finalize)
        {
            consultation.Complete();

            if (cmd.AppointmentId is not null)
            {
                try
                {
                    var appt = await appointments.GetByIdAsync(AppointmentId.From(cmd.AppointmentId), ct);
                    if (appt is not null)
                    {
                        appt.Complete(consultation.Id.ToString());
                        await appointments.UpdateAsync(appt, ct);
                    }
                    else
                    {
                        logger.LogWarning("Appointment {AppointmentId} not found when finalizing consultation {ConsultationId}.",
                            cmd.AppointmentId, consultation.Id);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, "Appointment status transition failed for {AppointmentId} while finalizing consultation {ConsultationId}.",
                        cmd.AppointmentId, consultation.Id);
                }
            }

            var invoiceNumber = await invoices.GenerateNumberAsync(ct);
            var invoice = Invoice.CreateFromConsultation(
                cmd.PatientId, consultation.Id.ToString(),
                currentUser.UserId, cmd.Tariff, invoiceNumber);
            await invoices.AddAsync(invoice, ct);

            var patient = await patients.GetByIdAsync(PatientId.From(cmd.PatientId), ct);
            patient?.RecordVisit();
            if (patient is not null) await patients.UpdateAsync(patient, ct);
        }

        if (isNew)
            await consultations.AddAsync(consultation, ct);
        else
            await consultations.UpdateAsync(consultation, ct);

        await audit.LogAsync("SaveConsultation", "Consultation", consultation.Id.ToString(),
            after: new { consultation.PatientId, consultation.Diagnosis, Finalized = cmd.Finalize }, ct: ct);

        return consultation.Id.ToString();
    }
}
