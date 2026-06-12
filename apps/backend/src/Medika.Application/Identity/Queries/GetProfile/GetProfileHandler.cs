using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Queries.GetProfile;

public class GetProfileHandler(
    IUserRepository users,
    ICurrentUserService currentUser) : ICommandHandler<GetProfileQuery, ProfileResult>
{
    public async Task<ProfileResult> ExecuteAsync(GetProfileQuery query, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(UserId.From(currentUser.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        return new ProfileResult(
            user.Id.ToString(),
            user.FirstName,
            user.LastName,
            user.Email,
            user.Specialty,
            user.OrderNumber,
            user.CabinetName,
            user.CabinetAddress,
            user.CabinetCity,
            user.CabinetWilaya,
            user.CabinetPhone,
            user.CreatedAt,
            user.LastLoginAt);
    }
}
