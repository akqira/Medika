using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Commands.MarkInvoicePaid;

public class MarkInvoicePaidHandler(IInvoiceRepository invoices, IAuditService audit)
    : ICommandHandler<MarkInvoicePaidCommand>
{
    public async Task ExecuteAsync(MarkInvoicePaidCommand cmd, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(InvoiceId.From(cmd.InvoiceId), ct)
            ?? throw new KeyNotFoundException($"Invoice {cmd.InvoiceId} not found.");

        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod, ignoreCase: true);
        invoice.MarkPaid(method);

        await invoices.UpdateAsync(invoice, ct);
        await audit.LogAsync("MarkInvoicePaid", "Invoice", invoice.Id.ToString(),
            after: new { invoice.Number, invoice.Amount, Method = cmd.PaymentMethod }, ct: ct);
    }
}
