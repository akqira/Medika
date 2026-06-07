using FastEndpoints;
using Medika.Domain.Medical;

namespace Medika.Application.Medical.Queries.GetPatientConsultations;

public class GetPatientConsultationsHandler(
    IConsultationRepository consultations) : ICommandHandler<GetPatientConsultationsQuery, ConsultationListResult>
{
    public async Task<ConsultationListResult> ExecuteAsync(GetPatientConsultationsQuery query, CancellationToken ct)
    {
        var (items, total) = await consultations.GetByPatientPagedAsync(query.PatientId, query.Page, query.PageSize, ct);

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
