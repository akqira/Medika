using FastEndpoints;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.Register;

public record RegisterCommand(
    string Email, string Password,
    string FirstName, string LastName,
    Role Role,
    string? Specialty = null,
    string? OrderNumber = null) : ICommand<string>;
