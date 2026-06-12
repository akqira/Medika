using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Queries.GetPatientInvoices;

public class GetPatientInvoicesHandler(IInvoiceRepository invoices, ICurrentUserService currentUser)
    : ICommandHandler<GetPatientInvoicesQuery, List<PatientInvoiceDto>>
{
    public async Task<List<PatientInvoiceDto>> ExecuteAsync(GetPatientInvoicesQuery query, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var items = await invoices.GetByPatientAsync(cabinetId, query.PatientId, ct);

        return items
            .Select(i => new PatientInvoiceDto(
                i.Id.ToString(),
                i.Number,
                i.ConsultationId,
                i.Amount,
                i.Status.ToString(),
                i.PaymentMethod?.ToString(),
                i.IssuedAt.ToString("o"),
                i.PaidAt?.ToString("o")))
            .ToList();
    }
}
