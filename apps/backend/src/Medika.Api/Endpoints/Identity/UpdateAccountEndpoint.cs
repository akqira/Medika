using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.UpdateAccount;

namespace Medika.Api.Endpoints.Identity;

public class UpdateAccountRequest
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
}

public class UpdateAccountValidator : Validator<UpdateAccountRequest>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class UpdateAccountEndpoint : Endpoint<UpdateAccountRequest>
{
    public override void Configure()
    {
        Patch("/api/profile/account");
        Roles("Doctor");
    }

    public override async Task HandleAsync(UpdateAccountRequest req, CancellationToken ct)
    {
        try
        {
            var cmd = new UpdateAccountCommand(req.FirstName, req.LastName, req.Email);
            await cmd.ExecuteAsync(ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
        }
    }
}
