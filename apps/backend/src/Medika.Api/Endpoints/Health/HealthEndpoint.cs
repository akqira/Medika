using FastEndpoints;

namespace Medika.Api.Endpoints.Health;

/// <summary>
/// Unauthenticated health probe — used by Azure App Service health checks
/// and exempt from ApiKeyMiddleware.
/// </summary>
public class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await HttpContext.Response.SendAsync(new HealthResponse("ok", DateTime.UtcNow), 200, null, ct);
    }
}

public record HealthResponse(string Status, DateTime Utc);
