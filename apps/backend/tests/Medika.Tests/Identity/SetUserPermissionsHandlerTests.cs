using Medika.Application.Authorization;
using Medika.Application.Identity.Commands.SetUserPermissions;
using Medika.Domain.Identity;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Identity;

public class SetUserPermissionsHandlerTests
{
    private const string Cabinet = "cabinet-1";

    private static SetUserPermissionsHandler Handler(FakeUserRepository repo, string cabinetId = Cabinet) =>
        new(repo, new FakeCurrentUserService { CabinetId = cabinetId }, new FakeAuditService());

    private static User Secretary(string cabinetId = Cabinet) =>
        User.Create("sec@x.com", "h", "S", "R", Role.Secretary, cabinetId: cabinetId,
            permissions: [PermissionConstants.Patients.View]);

    [Fact]
    public async Task Replaces_and_sanitizes_the_permission_set()
    {
        var user = Secretary();
        var repo = new FakeUserRepository(user);

        await Handler(repo).ExecuteAsync(
            new SetUserPermissionsCommand(user.Id.ToString(),
                [PermissionConstants.Scheduling.Manage, "forged"]), CancellationToken.None);

        Assert.Equal(new[] { PermissionConstants.Scheduling.Manage }, repo.Users[0].Permissions);
    }

    [Fact]
    public async Task A_doctors_permissions_cannot_be_modified()
    {
        var doctor = User.Create("doc@x.com", "h", "D", "O", Role.Doctor, cabinetId: Cabinet);
        var repo = new FakeUserRepository(doctor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler(repo).ExecuteAsync(
                new SetUserPermissionsCommand(doctor.Id.ToString(), []), CancellationToken.None));
    }

    [Fact]
    public async Task Cross_cabinet_target_is_not_found()
    {
        var user = Secretary(cabinetId: "other-cabinet");
        var repo = new FakeUserRepository(user);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Handler(repo).ExecuteAsync(
                new SetUserPermissionsCommand(user.Id.ToString(), []), CancellationToken.None));
    }
}
