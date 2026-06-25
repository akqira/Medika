using FastEndpoints;

namespace Medika.Api.Endpoints.Identity.Users;

public class GetPermissionMetadataEndpoint : EndpointWithoutRequest<IReadOnlyList<PermissionCategory>>
{
    public override void Configure()
    {
        Get("/api/permissions/metadata");
        Permissions(PermissionConstants.Users.View);
        Summary(s => s.Summary = "Category-grouped permission catalogue for the team UI");
    }

    public override async Task<IReadOnlyList<PermissionCategory>> ExecuteAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        return PermissionMetadata.Categories;
    }
}
