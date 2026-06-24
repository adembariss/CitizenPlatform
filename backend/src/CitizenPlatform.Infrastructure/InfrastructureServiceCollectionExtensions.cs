using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Infrastructure.Geospatial;
using CitizenPlatform.Infrastructure.Identity;
using CitizenPlatform.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenPlatform.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalObjectStorageService>();
        services.AddScoped<IGeospatialService, GeospatialService>();

        return services;
    }
}
