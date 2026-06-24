using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CitizenPlatform.Infrastructure.Persistence;

public sealed class CitizenPlatformDbContextFactory : IDesignTimeDbContextFactory<CitizenPlatformDbContext>
{
    public CitizenPlatformDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<CitizenPlatformDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options => options.UseNetTopologySuite());

        return new CitizenPlatformDbContext(optionsBuilder.Options);
    }
}
