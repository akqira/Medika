using FastEndpoints;

namespace Medika.Application.Finance.Commands.MarkInvoicePaid;

public record MarkInvoicePaidCommand(string InvoiceId, string PaymentMethod) : ICommand;
