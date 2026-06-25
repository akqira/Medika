using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;
using Medika.Domain.Medical;

namespace Medika.Tests.Fakes;

/// <summary>In-memory <see cref="IUserRepository"/> for Identity handler unit tests.</summary>
public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public FakeUserRepository(params User[] seed) => Users.AddRange(seed);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email.ToLowerInvariant()));

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(Users.Any(u => u.Email == email.ToLowerInvariant()));

    public Task<IReadOnlyList<User>> GetByCabinetAsync(string cabinetId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<User>>(Users.Where(u => u.CabinetId == cabinetId).ToList());

    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task AddAsync(User aggregate, CancellationToken ct = default)
    {
        Users.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User aggregate, CancellationToken ct = default)
    {
        var i = Users.FindIndex(u => u.Id == aggregate.Id);
        if (i >= 0) Users[i] = aggregate;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserId id, CancellationToken ct = default)
    {
        Users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }
}

/// <summary>Deterministic password hasher for tests (never real bcrypt).</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}

/// <summary>In-memory <see cref="ICurrentUserService"/> for handler unit tests.</summary>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    public string UserId { get; init; } = "user-1";
    public string CabinetId { get; init; } = "cabinet-1";
    public string UserName { get; init; } = "Test User";
    public string Role { get; init; } = "Doctor";
    public bool IsAuthenticated { get; init; } = true;
}

/// <summary>No-op <see cref="IAuditService"/> for handler unit tests.</summary>
public sealed class FakeAuditService : IAuditService
{
    public Task LogAsync(string action, string entityType, string? entityId = null,
        object? before = null, object? after = null, CancellationToken ct = default)
        => Task.CompletedTask;
}

/// <summary>
/// Fake consultation repository — only the paged-by-patient query is exercised by the
/// ADR-002 tests; the rest throw so an accidental call surfaces loudly.
/// Records the cabinetId it was queried with so tests can assert cabinet scoping.
/// </summary>
public sealed class FakeConsultationRepository : IConsultationRepository
{
    private readonly List<Consultation> _items;
    public string? LastQueriedCabinetId { get; private set; }
    public string? LastQueriedPatientId { get; private set; }

    public FakeConsultationRepository(IEnumerable<Consultation> items) => _items = items.ToList();

    public Task<(List<Consultation> Items, long Total)> GetByPatientPagedAsync(
        string cabinetId, string patientId, int page, int pageSize, CancellationToken ct = default)
    {
        LastQueriedCabinetId = cabinetId;
        LastQueriedPatientId = patientId;
        var matched = _items
            .Where(c => c.CabinetId == cabinetId && c.PatientId == patientId)
            .OrderByDescending(c => c.Date)
            .ToList();
        var page1 = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((page1, (long)matched.Count));
    }

    public Task<Consultation?> GetByIdAsync(ConsultationId id, CancellationToken ct = default) => throw new NotImplementedException();
    public Task AddAsync(Consultation aggregate, CancellationToken ct = default) => throw new NotImplementedException();
    public Task UpdateAsync(Consultation aggregate, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DeleteAsync(ConsultationId id, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<Consultation>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Consultation?> GetByAppointmentAsync(string cabinetId, string appointmentId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Consultation?> GetDraftAsync(string cabinetId, string patientId, string doctorId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Consultation?> GetByIdStringAsync(string cabinetId, string consultationId, CancellationToken ct = default) => throw new NotImplementedException();
}

/// <summary>Builds finalized consultations with a diagnosis for use in tests.</summary>
public static class ConsultationFactory
{
    public static Consultation WithDiagnosis(
        string cabinetId, string patientId,
        string reason = "Fièvre", string diagnosis = "Grippe saisonnière")
    {
        var c = Consultation.Start(cabinetId, patientId, doctorId: "doctor-1", reason);
        c.SetClinicalData(clinicalExam: "RAS", diagnosis: diagnosis, notes: "repos", vitalSigns: null);
        c.SetTariff(2000m);
        c.Complete();
        return c;
    }
}
