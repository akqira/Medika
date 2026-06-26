using Medika.Domain.Common;

namespace Medika.Domain.Identity;

public interface IUserRepository : IRepository<User, UserId>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>All users belonging to a cabinet (team roster), sorted by name. cabinetId-first (multi-tenancy rule).</summary>
    Task<IReadOnlyList<User>> GetByCabinetAsync(string cabinetId, CancellationToken ct = default);
}
