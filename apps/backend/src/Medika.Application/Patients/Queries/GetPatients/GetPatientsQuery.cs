using FastEndpoints;
using Medika.Application.Common.Models;

namespace Medika.Application.Patients.Queries.GetPatients;

public record GetPatientsQuery(string? Term, int Page = 1, int PageSize = 20)
    : ICommand<PagedResult<PatientSummary>>;

public record PatientSummary(
    string Id, string FirstName, string LastName,
    int Age, string Gender, string Phone,
    string? BloodGroup, string? LastVisitAt,
    int AllergyCount);
