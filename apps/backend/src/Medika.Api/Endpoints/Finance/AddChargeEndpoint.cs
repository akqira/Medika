using FastEndpoints;
using FluentValidation;
using Medika.Application.Finance.Commands.AddCharge;
using Medika.Domain.Finance;

namespace Medika.Api.Endpoints.Finance;

public class AddChargeRequest
{
    public string Category { get; init; } = null!;
    public string Description { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Date { get; init; } = null!;
    public bool IsRecurring { get; init; }
}

public class AddChargeValidator : Validator<AddChargeRequest>
{
    public AddChargeValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => Enum.TryParse<ChargeCategory>(c, ignoreCase: true, out _))
            .WithMessage(x => $"'{x.Category}' is not a valid charge category.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Date)
            .NotEmpty()
            .Must(d => DateOnly.TryParse(d, out _))
            .WithMessage("Date must be a valid date (yyyy-MM-dd).");
    }
}

public class AddChargeEndpoint : Endpoint<AddChargeRequest, AddChargeResponse>
{
    public override void Configure()
    {
        Post("/api/charges");
        Roles("Doctor");
    }

    public override async Task HandleAsync(AddChargeRequest req, CancellationToken ct)
    {
        var cmd = new AddChargeCommand(req.Category, req.Description, req.Amount, req.Date, req.IsRecurring);
        var id = await cmd.ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new AddChargeResponse(id), 201, null, ct);
    }
}

public record AddChargeResponse(string ChargeId);
