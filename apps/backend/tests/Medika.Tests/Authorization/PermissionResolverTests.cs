using Medika.Application.Authorization;
using Medika.Domain.Identity;
using Xunit;

namespace Medika.Tests.Authorization;

public class PermissionResolverTests
{
    private static User Doctor() =>
        User.Create("doc@x.com", "h", "Dee", "Oc", Role.Doctor, cabinetId: "cab-1");

    private static User Secretary(params string[] perms) =>
        User.Create("sec@x.com", "h", "Sec", "Retary", Role.Secretary, cabinetId: "cab-1", permissions: perms);

    [Fact]
    public void Doctor_resolves_to_every_permission()
    {
        var resolved = PermissionResolver.Resolve(Doctor());
        Assert.Equal(PermissionConstants.All.OrderBy(x => x), resolved.OrderBy(x => x));
    }

    [Fact]
    public void Secretary_resolves_to_exactly_their_stored_set()
    {
        var user = Secretary(PermissionConstants.Patients.View, PermissionConstants.Scheduling.Manage);
        var resolved = PermissionResolver.Resolve(user);
        Assert.Equal(
            new[] { PermissionConstants.Patients.View, PermissionConstants.Scheduling.Manage }.OrderBy(x => x),
            resolved.OrderBy(x => x));
    }

    [Fact]
    public void Unknown_permission_strings_are_dropped()
    {
        var user = Secretary(PermissionConstants.Patients.View, "forged_permission", "another_fake");
        var resolved = PermissionResolver.Resolve(user);
        Assert.Equal(new[] { PermissionConstants.Patients.View }, resolved);
    }

    [Fact]
    public void Doctor_create_never_stores_explicit_permissions()
    {
        // A Doctor's permissions are implicit — Create must not persist a stored set even if passed one.
        var doc = User.Create("d@x.com", "h", "D", "R", Role.Doctor, cabinetId: "cab-1",
            permissions: [PermissionConstants.Patients.View]);
        Assert.Empty(doc.Permissions);
        // …but the resolver still grants the full admin set.
        Assert.Equal(PermissionConstants.All.Count, PermissionResolver.Resolve(doc).Count);
    }
}
