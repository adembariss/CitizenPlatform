namespace CitizenPlatform.Api.Extensions;

public static class CorsOptionsResolver
{
    public const string PolicyName = "CitizenPlatformCors";

    private static readonly string[] DefaultDevelopmentOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:4173"
    ];

    public static string[] ResolveAllowedOrigins(IConfiguration configuration)
    {
        var raw = configuration["CORS:ALLOWED_ORIGINS"] ?? configuration["Cors:AllowedOrigins"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultDevelopmentOrigins;
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
