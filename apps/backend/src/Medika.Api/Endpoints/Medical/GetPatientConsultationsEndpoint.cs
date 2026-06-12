using FastEndpoints;
using Medika.Application.Medical.Queries.GetPatientConsultations;

namespace Medika.Api.Endpoints.Medical;

public class GetPatientConsultationsRequest
{
    public string Id { get; init; } = null!;
    [QueryParam] public int Page { get; init; } = 1;
    [QueryParam] public int PageSize { get; init; } = 20;
}

public class GetPatientConsultationsEndpoint : Endpoint<GetPatientConsultationsRequest, ConsultationListResult>
{
    public override void Configure()
    {
        Get("/api/patients/{id}/consultations");
        // ADR-002: receptionists may call this; the handler returns metadata-only for them.
        Roles("Doctor", "Receptionist");
    }

    public override async Task<ConsultationListResult> ExecuteAsync(GetPatientConsultationsRequest req, CancellationToken ct)
    {
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize is < 1 or > 100 ? 20 : req.PageSize