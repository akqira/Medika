using FastEndpoints;
using FluentValidation;
using Medika.Application.Finance.Commands.AddAct;

namespace Medika.Api.Endpoints.Finance;

public class AddActRequest
{
    public string Name { get; init; } = null!;
    public decimal Tariff { get; init; }
}

public class AddActValidator : Validator<AddActRequest>
{
    public AddActValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Tariff).GreaterThanOrEqualTo(0).WithMessage("Tariff must be zero or positive.");
    }
}

public class AddActEndpoint : Endpoint<AddActRequest, AddActResponse>
{
    public override void Configure()
    {
        Post("/api/acts");
        Roles("Doctor");
    }

    public override async Task HandleAsync(AddActRequest req, CancellationToken ct)
    {
        var id = await new AddActCommand(req.Name, req.Tariff).ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new AddActResponse(id), 201, null, ct);
    }
}

public record AddActResponse(string ActId);
