namespace Medika.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        object? before = null,
        object? after = null,
        CancellationToken ct = default);
}
