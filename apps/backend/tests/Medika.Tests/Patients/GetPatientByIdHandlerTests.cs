using Medika.Application.Common.Interfaces;
using Medika.Application.Patients.Queries.GetPatientById;
using Medika.Domain.Patients;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Patients;

/// <summary>
/// A malformed patient id must resolve to not-found (→ 404), never blow up as a 500.
/// (Architecture review Finding 3 — surfaced by a hand-typed bad GUID in the URL.)
/// </summary>
public class GetPatientByIdHandlerTests
{
    private sealed class ThrowingPatientRepository : IPatientRepository
    {
        public bool WasQueried { get; private set; }
        public Task<Patient?> GetByIdAsync(PatientId id, CancellationToken ct = default)
        {
            WasQueried = true;
            return Task.FromResult<Patient?>(null);
        }
        public Task AddAsync(Patient a, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Patient a, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(PatientId id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Patient>> SearchAsync(string c, string? t, int p, int s, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountAsync(string c, string? t, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Patient>> GetRecentAsync(string c, int n, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("f8dd19af-801d-450e-a087-fa2d11e57416213")] // one digit too long — the exact shape from the bug report
    [InlineData("")]
    public async Task Malformed_id_returns_null_without_touching_the_repository(string badId)
    {
        var repo = new ThrowingPatientRepository();
        var user = new FakeCurrentUserService { Role = "Doctor", CabinetId = "cabinet-1" };
        var handler = new GetPatientByIdHandler(repo, user);

        var result = await handler.ExecuteAsync(new GetPatientByIdQuery(badId), CancellationToken.None);

        Assert.Null(result);                 // → endpoint sends 404
        Assert.False(repo.WasQueried);       // parse failed before any DB call
    }

    [Fact]
    public async Task Empty_cabinet_claim_is_rejected()
    {
        var handler = new GetPatientByIdHandler(
            new ThrowingPatientRepository(),
            new FakeCurrentUserService { Role = "Doctor", CabinetId = "" });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.ExecuteAsync(new GetPatientByIdQuery(Guid.NewGuid().ToString()), CancellationToken.None));
    }
}
