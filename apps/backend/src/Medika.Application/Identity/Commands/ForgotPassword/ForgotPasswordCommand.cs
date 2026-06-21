using FastEndpoints;

namespace Medika.Application.Identity.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand;
