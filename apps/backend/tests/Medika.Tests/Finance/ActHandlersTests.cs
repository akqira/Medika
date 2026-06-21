using Medika.Application.Finance.Commands.AddAct;
using Medika.Application.Finance.Commands.DeleteAct;
using Medika.Domain.Finance;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Finance;

public class ActHandlersTests
{
    // In-memory IActRepository for handler tests.
    private sealed class FakeActRepository : IActRepository
    {
        public readonly List<Act> Items = [];

        public Task<IReadOnlyList<Act>> GetByCabinetAsync(string cabinetId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Act>>(Items.Where(a => a.CabinetId == cabinetId).ToList());

        public Task<Act?> GetByIdAsync(ActId id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(a => a.Id.Value == id.Value));

        public Task AddAsync(Act aggregate, CancellationToken ct = default)
        {
            Items.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ActId id, CancellationToken ct = default)
        {
            Items.RemoveAll(a => a.Id.Value == id.Value);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Act aggregate, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task Add_persists_act_scoped_to_current_cabinet()
    {
        var repo = new FakeActRepository();
        var handler = new AddActHandler(repo, new FakeCurrentUserService { CabinetId = "cab-A" }, new FakeAuditService());

        var id = await handler.ExecuteAsync(new AddActCommand("Consultation", 2000m), CancellationToken.None);

        var act = Assert.Single(repo.Items);
        Assert.Equal(id, act.Id.ToString());
        Assert.Equal("cab-A", act.CabinetId);
        Assert.Equal("Consultation", act.Name);
        Assert.Equal(2000m, act.Tariff);
    }

    [Fact]
    public async Task Add_rejects_empty_cabinet_claim()
    {
        var handler = new AddActHandler(new FakeActRepository(), new FakeCurrentUserService { CabinetId = "" }, new FakeAuditService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.ExecuteAsync(new AddActCommand("Consultation", 2000m), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_removes_act_in_same_cabinet()
    {
        var repo = new FakeActRepository();
        var act = Act.Create("cab-A", "Consultation", 2000m);
        repo.Items.Add(act);
        var handler = new DeleteActHandler(repo, new FakeCurrentUserService { CabinetId = "cab-A" }, new FakeAuditService());

        await handler.ExecuteAsync(new DeleteActCommand(act.Id.ToString()), CancellationToken.None);

        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task Delete_across_cabinets_is_not_found_and_keeps_the_act()
    {
        var repo = new FakeActRepository();
        var act = Act.Create("cab-A", "Consultation", 2000m); // belongs to another cabinet
        repo.Items.Add(act);
        var handler = new DeleteActHandler(repo, new FakeCurrentUserService { CabinetId = "cab-B" }, new FakeAuditService());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.ExecuteAsync(new DeleteActCommand(act.Id.ToString()), CancellationToken.None));
        Assert.Single(repo.Items); // not deleted
    }
}
