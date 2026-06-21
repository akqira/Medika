using FastEndpoints;

namespace Medika.Application.Identity.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand;
