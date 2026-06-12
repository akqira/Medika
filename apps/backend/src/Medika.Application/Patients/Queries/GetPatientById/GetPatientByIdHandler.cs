using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Patients;

namespace Medika.Application.Patients.Queries.GetPatientById;

public class GetPatientByIdHandler(
    IPatientRepository patients,
    ICurrentUserService currentUser)
    : ICommandHandler<GetPatientByIdQuery, PatientDetail?>
{
    public async Task<PatientDetail?> ExecuteAsync(GetPatientByIdQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        // A malformed id is treated as not-found (→ 404), never a 500.
        var patientId = PatientId.TryFrom(query.Id);
        if (patientId is null) return null;

        var patient = await patients.GetByIdAsync(patientId, ct);
        if (patient is null) return null;

        // Cabinet guard — treat cross-cabinet access as not-found (no information leak).
        if (!string.IsNullOrEmpty(patient.CabinetId) && patient.CabinetId != cabinetId)
            return null;

        return new PatientDetail(
            patient.Id.ToString(),
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth.ToString("yyyy-MM-dd"),
            patient.Age,
            patient.Gender,
            patient.Phone,
            patient.Email,
            patient.Address,
            patient.Nss,
            patient.BloodGroup?.ToString(),
            patient.Wilaya,
            patient.EmergencyContactName,
            patient.EmergencyContactPhone,
            patient.InsuranceProvider,
            patient.MutualInsurance,
            patient.CurrentTreatment,
            