using Microsoft.Extensions.Configuration;

namespace CitizenPlatform.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string DefaultDevelopmentSecret = "change-me-local-development-secret-please-replace";

    public string Issuer { get; init; } = "citizen-platform";

    public string Audience { get; init; } = "citizen-platform";

    public string Secret { get; init; } = DefaultDevelopmentSecret;

    public int AccessTokenMinutes { get; init; } = 60;

    public int RefreshTokenDays { get; init; } = 14;

    /// <summary>
    /// Fails fast outside Development when the JWT secret is missing, left at the well-known
    /// development default, or too short to be safe for HMAC-SHA256.
    /// </summary>
    public static void EnsureProductionSecret(IConfiguration configuration)
    {
        var secret = Resolve(configuration).Secret;

        if (string.Equals(secret, DefaultDevelopmentSecret, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "JWT secret is still the development default. Set JWT__SECRET (or Jwt:Secret) to a strong random value before running outside Development.");
        }

        if (secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT secret must be at least 32 characters long outside Development.");
        }
    }

    public static JwtOptions Resolve(IConfiguration configuration)
    {
        return new JwtOptions
        {
            Issuer = configuration["JWT:ISSUER"] ?? configuration["Jwt:Issuer"] ?? "citizen-platform",
            Audience = configuration["JWT:AUDIENCE"] ?? configuration["Jwt:Audience"] ?? "citizen-platform",
            Secret = configuration["JWT:SECRET"] ?? configuration["Jwt:Secret"] ?? DefaultDevelopmentSecret,
            AccessTokenMinutes = ReadAccessTokenMinutes(configuration),
            RefreshTokenDays = ReadRefreshTokenDays(configuration)
        };
    }

    private static int ReadAccessTokenMinutes(IConfiguration configuration)
    {
        var raw = configuration["JWT:ACCESS_TOKEN_MINUTES"] ?? configuration["Jwt:AccessTokenMinutes"];
        return int.TryParse(raw, out var minutes) && minutes > 0 ? minutes : 60;
    }

    private static int ReadRefreshTokenDays(IConfiguration configuration)
    {
        var raw = configuration["JWT:REFRESH_TOKEN_DAYS"] ?? configuration["Jwt:RefreshTokenDays"];
        return int.TryParse(raw, out var days) && days > 0 ? days : 14;
    }
}
