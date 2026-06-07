using FastEndpoints;
using Medika.Application.Identity.Queries.GetProfile;

namespace Medika.Api.Endpoints.Identity;

public class GetProfileEndpoint : EndpointWithoutRequest<ProfileResult>
{
    public override void Configure()
    {
        Get("/api/profile");
        Roles("Doctor");
    }

    public override async Task<ProfileResult> ExecuteAsync(CancellationToken ct)
    {
        return await new GetProfileQuery().ExecuteAsync(ct);
    }
}
