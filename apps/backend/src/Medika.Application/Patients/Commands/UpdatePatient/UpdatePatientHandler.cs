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

        var id = PatientId.From(cmd.Id);
        var patient = await patients.GetByIdAsync(id, ct);

        if (patient is null ||
            (!string.IsNullOrEmpty(patient.CabinetId) && patient.CabinetId != cabinetId))
            throw new KeyNotFoundException($"Patient '{cmd.Id}' not found.");

        var before = new { patient.FirstName, patient.LastName };

        patient.Update(
            cmd.FirstName, cmd.LastName,
            DateOnly.Parse(cmd.DateOfBirth), cmd.Gender,
            cmd.Phone,
            cmd.Email, cmd.Address, cmd.Nss,
            cmd.BloodGroup, cmd.Wilaya,
            cmd.EmergencyContactName, cmd.EmergencyContactPhone,
            cmd.InsuranceProvider, cmd.MutualInsurance,
            cmd.CurrentTreatment,
            cmd.Allergies ?? [],
            cmd.MedicalHistory ?? []);

        await patients.UpdateAsync(patient, ct);
        await audit.LogAsync("UpdatePatient", "Patient", cmd.Id,
            before: before,
            after: new { patient.FirstName, patient.LastName },
            ct: ct);
    }
}
