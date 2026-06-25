using Medika.Application.Authorization;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Medika.Infrastructure.Auth;

public class JwtTokenGenerator(JwtSettings settings) : IJwtTokenGenerator
{
    public string Generate(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("fullName", $"{user.FirstName} {user.LastName}"),
            new("cabinetId", user.CabinetId ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Effective permissions (Doctor → all; staff → their customisable set). FastEndpoints
        // reads these from the "permissions" claim type to enforce Permissions(...) ACL guards.
        // Editing a staff member's permissions takes effect on their next login (stateless JWT),
        // consistent with the cabinetId re-login doctrine.
        foreach (var permission in PermissionResolver.Resolve(user))
            claims.Add(new Claim("permissions", permission));

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
