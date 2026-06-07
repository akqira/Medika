using FastEndpoints;
using Medika.Application.Identity.Commands.ChangePassword;

namespace Medika.Api.Endpoints.Identity;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
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
        if (string.IsNullOrWhiteSpace(req.CurrentPassword))
            AddError(nameof(req.CurrentPassword), "Current password is required.");
        if (string.IsNullOrWhiteSpace(req.NewPassword))
            AddError(nameof(req.NewPassword), "New password is required.");
        if (req.NewPassword != req.ConfirmPassword)
            AddError(nameof(req.ConfirmPassword), "Passwords do not match.");

        if (ValidationFailed)
        {
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
            return;
        }

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
