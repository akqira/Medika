using FastEndpoints;

namespace Medika.Application.Identity.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommand<LoginResult>;

public record LoginResult(string Token, string UserId, string Role, string FullName);
