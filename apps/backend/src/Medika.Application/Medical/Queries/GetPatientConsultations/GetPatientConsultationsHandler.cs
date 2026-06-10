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

        var summaries = items
            .Select(c => new ConsultationSummary(
                c.Id.ToString(),
                c.Date,
                c.Reason,
                c.Diagnosis,
                c.Tariff,
                c.IsFinalized,
                c.Prescription.Count,
                c.AppointmentId))
            .ToList();

        return new ConsultationListResult(query.PatientId, total, query.Page, query.PageSize, summaries);
    }
}
