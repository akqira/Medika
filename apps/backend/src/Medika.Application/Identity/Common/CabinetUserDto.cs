namespace Medika.Application.Identity.Common;

/// <summary>A team member as shown on the cabinet's "Équipe" management screen.</summary>
public record CabinetUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    IReadOnlyList<string> Permissions,
    DateTime? LastLoginAt);
