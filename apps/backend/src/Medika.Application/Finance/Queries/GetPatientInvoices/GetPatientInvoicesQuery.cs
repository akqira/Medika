using FastEndpoints;

namespace Medika.Application.Finance.Queries.GetPatientInvoices;

public record GetPatientInvoicesQuery(string PatientId) : ICommand<List<PatientInvoiceDto>>;

public record PatientInvoiceDto(
    string Id,
    string Number,
    string ConsultationId,
    decimal Amount,
    string Status,
    string? PaymentMethod,
    string IssuedAt,
    string? PaidAt);
