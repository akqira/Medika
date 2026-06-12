namespace Medika.Infrastructure.Auth;

public class JwtSettings
{
    public const string Section = "Jwt";
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "medika-api";
    public string Audience { get; set; } = "medika-app";
    public int ExpiryMinutes { get; set; } = 480;   // 8h
}
