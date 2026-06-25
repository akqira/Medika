using System.IdentityModel.Tokens.Jwt;
using Medika.Application.Authorization;
using Medika.Domain.Identity;
using Medika.Infrastructure.Auth;
using Xunit;

namespace Medika.Tests.Infrastructure;

public class JwtTokenGeneratorTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "this-is-a-test-signing-secret-of-sufficient-length-1234567890",
        Issuer = "medika-api",
        Audience = "medika-app",
        ExpiryMinutes = 60
    };

    private static List<string> PermissionClaims(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Where(c => c.Type == "permissions").Select(c => c.Value).ToList();

    [Fact]
    public void Doctor_token_carries_every_permission()
    {
        var doctor = User.Create("doc@x.com", "h", "D", "O", Role.Doctor, cabinetId: "cab-1");
        var token = new JwtTokenGenerator(Settings).Generate(doctor);

        Assert.Equal(
            PermissionConstants.All.OrderBy(x => x),
            PermissionClaims(token).OrderBy(x => x));
    }

    [Fact]
    public void Secretary_token_carries_only_their_stored_permissions()
    {
        var secretary = User.Create("sec@x.com", "h", "S", "R", Role.Secretary, cabinetId: "cab-1",
            permissions: [PermissionConstants.Patients.View, PermissionConstants.Scheduling.View]);
        var token = new JwtTokenGenerator(Settings).Generate(secretary);

        Assert.Equal(
            new[] { PermissionConstants.Patients.View, PermissionConstants.Scheduling.View }.OrderBy(x => x),
            PermissionClaims(token).OrderBy(x => x));
    }
}
