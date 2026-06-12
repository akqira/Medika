using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Medical;

namespace Medika.Application.Medical.Queries.GetPatientConsultations;

public class GetPatientConsultationsHandler(
    IConsultationRepository consultations,
    ICurrentUserService currentUser) : ICommandHandler<GetPatientConsultationsQuery, ConsultationListResult>
{
    public async Task<ConsultationListResult> ExecuteAsync(GetPatientConsultationsQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var (items, total) = await consultations.GetByPatientPagedAsync(cabinetId, query.PatientId, query.Page, query.PageSize, ct);

        // ADR-002 — role-shaped projection. Doctors get the full clinical summary;
        // everyone else (Receptionist and any non-Doctor role) gets metadata only.
        // Fail safe: the full summary is returned ONLY for an explicit Doctor role.
        var isDoctor = string.Equals(currentUser.Role, "Doctor", StringComparison.Ordinal);

        IReadOnlyList<object> projected = isDoctor
            ? items.Select(c => (object)new ConsultationSummary(
   