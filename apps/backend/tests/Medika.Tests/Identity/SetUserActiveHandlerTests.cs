using Medika.Application.Identity.Commands.SetUserActive;
using Medika.Domain.Identity;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Identity;

public class SetUserActiveHandlerTests
{
    private const string Cabinet = "cabinet-1";

    private static SetUserActiveHandler Handler(FakeUserRepository repo, string currentUserId = "admin-1") =>
        new(repo, new FakeCurrentUserService { CabinetId = Cabinet, UserId = currentUserId }, new FakeAuditService());

    private static User Secretary() =>
        User.Create("sec@x.com", "h", "S", "R", Role.Secretary, cabinetId: Cabinet);

    [Fact]
    public async Task Deactivates_then_reactivates_a_secretary()
    {
        var user = Secretary();
        var repo = new FakeUserRepository(user);

        await Handler(repo).ExecuteAsync(new SetUserActiveCommand(user.Id.ToString(), false), CancellationToken.None);
        Assert.False(repo.Users[0].IsActive);

        await Handler(repo).ExecuteAsync(new SetUserActiveCommand(user.Id.ToString(), true), CancellationToken.None);
        Assert.True(repo.Users[0].IsActive);
    }

    [Fact]
    public async Task The_doctor_admin_cannot_be_deactivated()
    {
        var doctor = User.Create("doc@x.com", "h", "D", "O", Role.Doctor, cabinetId: Cabinet);
        var repo = new FakeUserRepository(doctor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler(repo).ExecuteAsync(new SetUserActiveCommand(doctor.Id.ToString(), false), CancellationToken.None));
    }

    [Fact]
    public async Task A_user_cannot_deactivate_themselves()
    {
        var user = Secretary();
        var repo = new FakeUserRepository(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler(repo, currentUserId: user.Id.ToString())
                .ExecuteAsync(new SetUserActiveCommand(user.Id.ToString(), false), CancellationToken.None));
    }
}
