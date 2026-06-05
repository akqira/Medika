using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;

namespace Medika.Application.Medical.Commands.SaveConsultation;

public class SaveConsultationHandler(
    IConsultationRepository consultations,
    IAppointmentRepository appointments,
    IInvoiceRepository invoices,
    IPatientRepository patients,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<SaveConsultationCommand, string>
{
    public async Task<string> ExecuteAsync(SaveConsultationCommand cmd, CancellationToken ct)
    {
        var consultation = Consultation.Start(
            cmd.PatientId, currentUser.UserId,
            cmd.Reason, cmd.AppointmentId);

        var vitalSigns = cmd.VitalSigns is { } vs
            ? new VitalSigns(vs.BloodPressure, vs.PulseRate, vs.Weight, vs.Temperature, vs.SpO2)
            : null;

        consultation.SetClinicalData(cmd.ClinicalExam, cmd.Diagnosis, cmd.Notes, vitalSigns);

        foreach (var line in cmd.Prescription)
            consultation.AddPrescriptionLine(new PrescriptionLine(line.Medication, line.Dosage, line.Duration, line.Quantity));

        consultation.SetTariff(cmd.Tariff);

        if (cmd.Finalize)
        {
            consultation.Complete();

            if (cmd.AppointmentId is not null)
            {
                var appt = await appointments.GetByIdAsync(AppointmentId.From(cmd.AppointmentId), ct);
                appt?.Complete(consultation.Id.ToString());
                if (appt is not null) await appointments.UpdateAsync(appt, ct);
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

        await consultations.AddAsync(consultation, ct);
        await audit.LogAsync("SaveConsultation", "Consultation", consultation.Id.ToString(),
            after: new { consultation.PatientId, consultation.Diagnosis, Finalized = cmd.Finalize }, ct: ct);

        return consultation.Id.ToString();
    }
}
