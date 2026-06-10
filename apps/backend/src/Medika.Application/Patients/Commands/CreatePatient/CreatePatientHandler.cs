using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Patients;

namespace Medika.Application.Patients.Commands.CreatePatient;

public class CreatePatientHandler(
    IPatientRepository patients,
    ICurrentUserService currentUser,
    IAuditService audit) : ICommandHandler<CreatePatientCommand, string>
{
    public async Task<string> ExecuteAsync(CreatePatientCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var dob = DateOnly.Parse(cmd.DateOfBirth);
        var patient = Patient.Create(
            cabinetId,
            cmd.FirstName, cmd.LastName, dob, cmd.Gender,
            cmd.Phone, cmd.Email, cmd.Address, cmd.Nss,
            cmd.BloodGroup, doctorId: currentUser.UserId,
            wilaya: cmd.Wilaya,
            emergencyContactName: cmd.EmergencyContactName,
            emergencyContactPhone: cmd.EmergencyContactPhone,
            insuranceProvider: cmd.InsuranceProvider,
            mutualInsurance: cmd.MutualInsurance,
            currentTreatment: cmd.CurrentTreatment);

        foreach (var allergy in cmd.Allergies ?? [])
            patient.AddAllergy(allergy);

        foreach (var entry in cmd.MedicalHistory ?? [])
            patient.AddMedicalHistory(entry);

        await patients.AddAsync(patient, ct);
        await audit.LogAsync("CreatePatient", "Patient", patient.Id.ToString(),
            after: new { patient.FirstName, patient.LastName }, ct: ct);

        return patient.Id.ToString();
    }
}
