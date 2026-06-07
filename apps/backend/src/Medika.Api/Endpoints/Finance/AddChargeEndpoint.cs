using FastEndpoints;
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

public class AddChargeEndpoint : Endpoint<AddChargeRequest, AddChargeResponse>
{
    public override void Configure()
    {
        Post("/api/charges");
        Roles("Doctor");
    }

    public override async Task HandleAsync(AddChargeRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<ChargeCategory>(req.Category, ignoreCase: true, out _))
            AddError(nameof(req.Category), $"'{req.Category}' is not a valid charge category.");
        if (string.IsNullOrWhiteSpace(req.Description))
            AddError(nameof(req.Description), "Description is required.");
        if (req.Amount <= 0)
            AddError(nameof(req.Amount), "Amount must be greater than zero.");
        if (!DateOnly.TryParse(req.Date, out _))
            AddError(nameof(req.Date), "Date must be a valid date (yyyy-MM-dd).");

        if (ValidationFailed)
        {
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
            return;
        }

        var cmd = new AddChargeCommand(req.Category, req.Description, req.Amount, req.Date, req.IsRecurring);
        var id = await cmd.ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new AddChargeResponse(id), 201, null, ct);
    }
}

public record AddChargeResponse(string ChargeId);
