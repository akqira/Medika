using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.ChangePassword;

namespace Medika.Api.Endpoints.Identity;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
}

public class ChangePasswordValidator : Validator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).WithMessage("New password must be at least 8 characters.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ChangePasswordEndpoint : Endpoint<ChangePasswordRequest>
{
    public override void Configure()
    {
        Post("/api/profile/change-password");
        Roles("Doctor");
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        try
        {
            var cmd = new ChangePasswordCommand(req.CurrentPassword, req.NewPassword, req.ConfirmPassword);
            await cmd.ExecuteAsync(ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            AddError(nameof(req.CurrentPassword), ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 422, null, ct);
        }
    }
}
