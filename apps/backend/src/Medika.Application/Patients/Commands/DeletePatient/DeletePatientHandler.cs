using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;
using Medika.Domain.Medical;
using Medika.Domain.Patients;

namespace Medika.Application.Patients.Commands.DeletePatient;

public class DeletePatientHandler(
    IPatientRepository patients,
    IConsultationRepository consultations,
    IInvoiceRepository invoices,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<DeletePatientCommand>
{
    public async Task ExecuteAsync(DeletePatientCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var id = PatientId.From(cmd.Id);
        var patient = await patients.GetByIdAsync(id, ct);
        // Cross-cabinet access is indistinguishable from not-found (no information leak).
        if (patient is null ||
            (!string.IsNullOrEmpty(patient.CabinetId) && patient.CabinetId != cabinetId))
            throw new KeyNotFoundException($"Patient '{cmd.Id}' not found.");

        // Data-integrity guard: never hard-delete a patient that has clinical or
        // financial history — those records would be orphaned. 400 Bad Request.
        var hasConsultations = (await consultations.GetByPatientAsync(cabinetId, cmd.Id, ct)).Count > 0;
        var hasInvoices = (await invoices.GetByPatientAsync(cabinetId, cmd.Id, ct)).Count > 0;
        if (hasConsultations || hasInvoices)
            throw new ArgumentException(
                "Ce patient a des consultations ou factures associées et ne peut pas être supprimé.");

        await patients.DeleteAsync(id, ct);
        await audit.LogAsync("DeletePatient", "Patient", cmd.Id,
            before: new { patient.FirstName, patient.LastName }, ct: ct);
    }
}
