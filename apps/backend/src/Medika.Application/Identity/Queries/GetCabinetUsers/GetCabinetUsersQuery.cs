using FastEndpoints;
using Medika.Application.Identity.Common;

namespace Medika.Application.Identity.Queries.GetCabinetUsers;

public record GetCabinetUsersQuery() : ICommand<IReadOnlyList<CabinetUserDto>>;
