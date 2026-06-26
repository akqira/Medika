using Medika.Application.Authorization;
using Medika.Application.Identity.Commands.CreateStaff;
using Medika.Domain.Identity;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Identity;

public class CreateStaffHandlerTests
{
    private const string Cabinet = "cabinet-1";

    private static CreateStaffHandler Handler(FakeUserRepository repo, string cabinetId = Cabinet) =>
        new(repo, new FakeCurrentUserService { CabinetId = cabinetId }, new FakeAuditService(), new FakePasswordHasher());

    [Fact]
    public async Task Creates_a_secretary_in_the_current_cabinet()
    {
        var repo = new FakeUserRepository();
        var id = await Handler(repo).ExecuteAsync(
            new CreateStaffCommand("New@Staff.com", "password1", "New", "Staff", Permissions: null),
            CancellationToken.None);

        var created = Assert.Single(repo.Users);
        Assert.Equal(id, created.Id.ToString());
        Assert.Equal(Role.Secretary, created.Role);
        Assert.Equal(Cabinet, created.CabinetId);
        Assert.Equal("new@staff.com", created.Email); // normalized
    }

    [Fact]
    public async Task Null_permissions_apply_the_default_secretary_set()
    {
        var repo = new FakeUserRepository();
        await Handler(repo).ExecuteAsync(
            new CreateStaffCommand("a@b.com", "password1", "A", "B", Permissions: null), CancellationToken.None);

        Assert.Equal(
            PermissionConstants.DefaultSecretary.OrderBy(x => x),
            repo.Users[0].Permissions.OrderBy(x => x));
    }

    [Fact]
    public async Task Custom_permissions_are_sanitized()
    {
        var repo = new FakeUserRepository();
        await Handler(repo).ExecuteAsync(
            new CreateStaffCommand("a@b.com", "password1", "A", "B",
                Permissions: [PermissionConstants.Patients.View, "forged"]), CancellationToken.None);

        Assert.Equal(new[] { PermissionConstants.Patients.View }, repo.Users[0].Permissions);
    }

    [Fact]
    public async Task Duplicate_email_throws()
    {
        var existing = User.Create("dupe@x.com", "h", "D", "U", Role.Secretary, cabinetId: Cabinet);
        var repo = new FakeUserRepository(existing);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler(repo).ExecuteAsync(
                new CreateStaffCommand("dupe@x.com", "password1", "X", "Y", null), CancellationToken.None));
    }

    [Fact]
    public async Task Missing_cabinet_claim_throws()
    {
        var repo = new FakeUserRepository();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Handler(repo, cabinetId: "").ExecuteAsync(
                new CreateStaffCommand("a@b.com", "password1", "A", "B", null), CancellationToken.None));
    }
}
