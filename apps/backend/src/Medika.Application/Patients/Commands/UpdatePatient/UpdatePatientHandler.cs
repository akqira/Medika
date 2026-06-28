using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Patients;

namespace Medika.Application.Patients.Commands.UpdatePatient;

public class UpdatePatientHandler(
    IPatientRepository patients,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<UpdatePatientCommand>
{
    public async Task ExecuteAsync(UpdatePatientCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        // A malformed id is treated as not-found (→ 404), never a 500.
        var patientId = PatientId.TryFrom(cmd.Id)
            ?? throw new KeyNotFoundException($"Patient '{cmd.Id}' not found.");

        var patient = await patients.GetByIdAsync(patientId, ct);
        // Cross-cabinet access is indistinguishable from not-found (no information leak).
        if (patient is null ||
            (!string.IsNullOrEmpty(patient.CabinetId) && patient.CabinetId != cabinetId))
            throw new KeyNotFoundException($"Patient '{cmd.Id}' not found.");

        var before = new { patient.FirstName, patient.LastName, patient.Phone };

        var dob = DateOnly.Parse(cmd.DateOfBirth);
        patient.Update(
            cmd.FirstName, cmd.LastName, dob, cmd.Gender,
            cmd.Phone, cmd.Email, cmd.Address, cmd.Nss, cmd.BloodGroup,
            cmd.Wilaya, cmd.EmergencyContactName, cmd.EmergencyContactPhone,
            cmd.InsuranceProvider, cmd.MutualInsurance, cmd.CurrentTreatment,
            cmd.Allergies ?? [], cmd.MedicalHistory ?? []);

        await patients.UpdateAsync(patient, ct);
        await audit.LogAsync("UpdatePatient", "Patient", patient.Id.ToString(),
            before: before,
            after: new { patient.FirstName, patient.LastName, patient.Phone }, ct: ct);
    }
}
