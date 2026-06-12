using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Application.Common.Models;
using Medika.Domain.Patients;

namespace Medika.Application.Patients.Queries.GetPatients;

public class GetPatientsHandler(
    IPatientRepository patients,
    ICurrentUserService currentUser)
    : ICommandHandler<GetPatientsQuery, PagedResult<PatientSummary>>
{
    public async Task<PagedResult<PatientSummary>> ExecuteAsync(GetPatientsQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var items = await patients.SearchAsync(cabinetId, query.Term, query.Page, query.PageSize, ct);
        var total = await patients.CountAsync(cabinetId, query.Term, ct);

        var summaries = items.Select(p => new PatientSummary(
            p.Id.ToString(), p.FirstName, p.LastName,
            p.Age, p.Gender, p.Phone,
            p.BloodGroup?.ToString(),
            p.LastVisitAt?.ToString("dd/MM/yyyy"),
            p.Allergies.Count)).ToList();

        return new PagedResult<PatientSummary>(summaries, total, query.Page, query.PageSize);
    }
}
