using FastEndpoints;

namespace Medika.Application.Identity.Commands.CreateStaff;

/// <summary>
/// Creates a Secretary (front-desk staff) in the current doctor's cabinet, with an optional
/// custom permission set. When <see cref="Permissions"/> is null, a sensible default set is applied.
/// </summary>
public record CreateStaffCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    IReadOnlyList<string>? Permissions) : ICommand<string>;
