using FastEndpoints;

namespace Medika.Application.Identity.Commands.UpdateAccount;

public record UpdateAccountCommand(
    string FirstName,
    string LastName,
    string Email) : ICommand;
