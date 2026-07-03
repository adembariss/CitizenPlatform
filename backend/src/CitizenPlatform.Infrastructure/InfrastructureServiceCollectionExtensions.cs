using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Infrastructure.Geospatial;
using CitizenPlatform.Infrastructure.Identity;
using CitizenPlatform.Infrastructure.Persistence;
using CitizenPlatform.Infrastructure.Persistence.Repositories;
using CitizenPlatform.Infrastructure.Storage;
using CitizenPlatform.Infrastructure.Time;
using CitizenPlatform.Infrastructure.Tracking;
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

        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IFileSafetyScanner, NoOpFileSafetyScanner>();
        services.AddScoped<IImageMetadataReader, ExifImageMetadataReader>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IGeospatialService, GeospatialService>();
        services.AddScoped<IGeoMunicipalityBoundaryLookup, PostgisMunicipalityBoundaryLookup>();
        services.AddScoped<IGeoMunicipalityResolver, GeoMunicipalityResolver>();
        services.AddScoped<IComplaintRepository, ComplaintRepository>();
        services.AddScoped<IComplaintCategoryRepository, ComplaintCategoryRepository>();
        services.AddScoped<ICategoryDepartmentRuleRepository, CategoryDepartmentRuleRepository>();
        services.AddScoped<ICitizenRepository, CitizenRepository>();
        services.AddScoped<IIntegrationOutboxRepository, IntegrationOutboxRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ITrackingCodeGenerator, TrackingCodeGenerator>();

        return services;
    }
}
