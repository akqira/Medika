using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Medika.Infrastructure.Audit;

public class AuditLog
{
    [BsonId, BsonRepresentation(BsonType.String)]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string Action { get; init; } = null!;
    public string EntityType { get; init; } = null!;
    public string? EntityId { get; init; }
    public BsonDocument? Before { get; init; }
    public BsonDocument? After { get; init; }
    public string? IpAddress { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
