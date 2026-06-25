using FastEndpoints;
using Medika.Application.Identity.Common;
using Medika.Application.Identity.Queries.GetCabinetUsers;

namespace Medika.Api.Endpoints.Identity.Users;

public class GetCabinetUsersEndpoint : EndpointWithoutRequest<IReadOnlyList<CabinetUserDto>>
{
    public override void Configure()
    {
        Get("/api/users");
        Permissions(PermissionConstants.Users.View);
        Summary(s => s.Summary = "List the cabinet's team members");
    }

    public override async Task<IReadOnlyList<CabinetUserDto>> ExecuteAsync(CancellationToken ct) =>
        await new GetCabinetUsersQuery().ExecuteAsync(ct);
}
