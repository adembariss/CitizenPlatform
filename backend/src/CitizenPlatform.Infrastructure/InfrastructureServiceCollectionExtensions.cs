using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Infrastructure.Geospatial;
using CitizenPlatform.Infrastructure.Identity;
using CitizenPlatform.Infrastructure.Persistence;
using CitizenPlatform.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenPlatform.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        services.AddDbContext<CitizenPlatformDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalObjectStorageService>();
        services.AddScoped<IGeospatialService, GeospatialService>();
        services.AddScoped<IGeoMunicipalityBoundaryLookup, PostgisMunicipalityBoundaryLookup>();
        services.AddScoped<IGeoMunicipalityResolver, GeoMunicipalityResolver>();

        return services;
    }
}
