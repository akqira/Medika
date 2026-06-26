using Medika.Domain.Identity;

namespace Medika.Application.Authorization;

/// <summary>
/// Resolves the effective permission set for a user. A Doctor is the cabinet administrator and
/// implicitly holds every permission; any other role holds exactly its stored, customisable set.
/// The result is what gets embedded as <c>permissions</c> claims in the JWT at login.
/// </summary>
public static class PermissionResolver
{
    public static IReadOnlyList<string> Resolve(User user) =>
        user.Role == Role.Doctor
            ? PermissionConstants.All
            : [.. PermissionConstants.Sanitize(user.Permissions)];
}
