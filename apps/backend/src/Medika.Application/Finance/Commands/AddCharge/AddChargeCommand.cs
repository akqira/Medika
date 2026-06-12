using FastEndpoints;

namespace Medika.Application.Finance.Commands.AddCharge;

public record AddChargeCommand(
    string Category,     // "Rent" | "Internet" | etc.
    string Description,
    decimal Amount,
    string Date,         // "2026-06-01"
    bool IsRecurring = false) : ICommand<string>;
