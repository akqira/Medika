using FastEndpoints;

namespace Medika.Application.Identity.Commands.ChangePassword;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : ICommand;
