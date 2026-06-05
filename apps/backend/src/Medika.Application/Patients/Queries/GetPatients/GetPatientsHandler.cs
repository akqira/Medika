using FastEndpoints;
using Medika.Application.Common.Models;
using Medika.Domain.Patients;

namespace Medika.Application.Patients.Queries.GetPatients;

public class GetPatientsHandler(IPatientRepository patients)
    : ICommandHandler<GetPatientsQuery, PagedResult<PatientSummary>>
{
    public async Task<PagedResult<PatientSummary>> ExecuteAsync(GetPatientsQuery query, CancellationToken ct)
    {
        var items = await patients.SearchAsync(query.Term, query.Page, query.PageSize, ct);
        var total = await patients.CountAsync(query.Term, ct);

        var summaries = items.Select(p => new PatientSummary(
            p.Id.ToString(), p.FirstName, p.LastName,
            p.Age, p.Gender, p.Phone,
            p.BloodGroup?.ToString(),
            p.LastVisitAt?.ToString("dd/MM/yyyy"),
            p.Allergies.Count)).ToList();

        return new PagedResult<PatientSummary>(summaries, total, query.Page, query.PageSize);
    }
}
