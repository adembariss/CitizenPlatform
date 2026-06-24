using Microsoft.Extensions.Configuration;

namespace CitizenPlatform.Infrastructure.Persistence;

public static class DatabaseConnectionStringResolver
{
    private const string DefaultConnectionName = "CitizenPlatform";

    public static string Resolve(IConfiguration configuration)
    {
        return configuration.GetConnectionString(DefaultConnectionName)
            ?? configuration["Database:ConnectionString"]
            ?? configuration["MAIN_DB_CONNECTION_STRING"]
            ?? "Host=localhost;Port=5432;Database=citizen_platform;Username=citizen_platform;Password=change-me-local";
    }
}
