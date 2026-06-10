using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Commands.MarkInvoicePaid;

public class MarkInvoicePaidHandler(IInvoiceRepository invoices, ICurrentUserService currentUser, IAuditService audit)
    : ICommandHandler<MarkInvoicePaidCommand>
{
    public async Task ExecuteAsync(MarkInvoicePaidCommand cmd, CancellationToken ct)
    {
        var cabinetId = currentUser.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
            throw new UnauthorizedAccessException("Missing cabinet claim — please re-login.");

        var invoice = await invoices.GetByIdAsync(InvoiceId.From(cmd.InvoiceId), ct)
            ?? throw new KeyNotFoundException($"Invoice {cmd.InvoiceId} not found.");

        // Cabinet guard — cross-cabinet access is indistinguishable from not-found (no information leak).
        if (!string.IsNullOrEmpty(invoice.CabinetId) && invoice.CabinetId != cabinetId)
            throw new KeyNotFoundException($"Invoice {cmd.InvoiceId} not found.");

        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod, ignoreCase: true);
        invoice.MarkPaid(method);

        await invoices.UpdateAsync(invoice, ct);
        await audit.LogAsync("MarkInvoicePaid", "Invoice", invoice.Id.ToString(),
            after: new { invoice.Number, invoice.Amount, Method = cmd.PaymentMethod }, ct: ct);
    }
}
