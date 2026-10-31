namespace CalorieTracker.Identity.Infrastructure.Tokens;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "calorie-tracker-identity";

    public string Audience { get; set; } = "calorie-tracker";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    // RSA private key in PEM format; required outside Development
    public string? SigningKey { get; set; }
}
