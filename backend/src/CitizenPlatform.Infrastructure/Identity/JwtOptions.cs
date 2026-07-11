using Microsoft.Extensions.Configuration;

namespace CitizenPlatform.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string DefaultDevelopmentSecret = "change-me-local-development-secret-please-replace";

    public string Issuer { get; init; } = "citizen-platform";

    public string Audience { get; init; } = "citizen-platform";

    public string Secret { get; init; } = DefaultDevelopmentSecret;

    public int AccessTokenMinutes { get; init; } = 60;

    public static JwtOptions Resolve(IConfiguration configuration)
    {
        return new JwtOptions
        {
            Issuer = configuration["JWT:ISSUER"] ?? configuration["Jwt:Issuer"] ?? "citizen-platform",
            Audience = configuration["JWT:AUDIENCE"] ?? configuration["Jwt:Audience"] ?? "citizen-platform",
            Secret = configuration["JWT:SECRET"] ?? configuration["Jwt:Secret"] ?? DefaultDevelopmentSecret,
            AccessTokenMinutes = ReadAccessTokenMinutes(configuration)
        };
    }

    private static int ReadAccessTokenMinutes(IConfiguration configuration)
    {
        var raw = configuration["JWT:ACCESS_TOKEN_MINUTES"] ?? configuration["Jwt:AccessTokenMinutes"];
        return int.TryParse(raw, out var minutes) && minutes > 0 ? minutes : 60;
    }
}
