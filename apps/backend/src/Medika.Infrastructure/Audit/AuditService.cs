using Medika.Application.Common.Interfaces;
using Medika.Infrastructure.Persistence;
using MongoDB.Bson;
using System.Text.Json;

namespace Medika.Infrastructure.Audit;

public class AuditService(MongoContext ctx, ICurrentUserService currentUser) : IAuditService
{
    public async Task LogAsync(
        string action, string entityType, string? entityId = null,
        object? before = null, object? after = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = currentUser.IsAuthenticated ? currentUser.UserId : "system",
            UserName = currentUser.IsAuthenticated ? currentUser.UserName : "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before is not null ? BsonDocument.Parse(JsonSerializer.Serialize(before)) : null,
            After = after is not null ? BsonDocument.Parse(JsonSerializer.Serialize(after)) : null,
        };
        await ctx.AuditLogs.InsertOneAsync(log, cancellationToken: ct);
    }
}
