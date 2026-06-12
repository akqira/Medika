using Medika.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Medika.Infrastructure.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub") ?? string.Empty;

    public string CabinetId => User?.FindFirstValue("cabinetId") ?? string.Empty;

    public string UserName => User?.FindFirstValue("fullName")
        ?? User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated is true;
}
