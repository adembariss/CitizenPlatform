using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Infrastructure.Geospatial;
using CitizenPlatform.Infrastructure.Identity;
using CitizenPlatform.Infrastructure.Persistence;
using CitizenPlatform.Infrastructure.Persistence.Repositories;
using CitizenPlatform.Infrastructure.Seeding;
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

        services.AddHttpContextAccessor();
        services.AddSingleton(JwtOptions.Resolve(configuration));
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IFileSafetyScanner, NoOpFileSafetyScanner>();
        services.AddScoped<IImageMetadataReader, ExifImageMetadataReader>();
        var storageProvider = configuration
            .GetSection(ObjectStorageOptions.SectionName)
            .GetValue<string>(nameof(ObjectStorageOptions.Provider));

        if (string.Equals(storageProvider, "Minio", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFileStorageService, MinioFileStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }
        services.AddScoped<IGeospatialService, GeospatialService>();
        services.AddScoped<IGeoMunicipalityBoundaryLookup, PostgisMunicipalityBoundaryLookup>();
        services.AddScoped<IGeoMunicipalityResolver, GeoMunicipalityResolver>();
        services.AddScoped<IComplaintRepository, ComplaintRepository>();
        services.AddScoped<IComplaintCategoryRepository, ComplaintCategoryRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ICategoryDepartmentRuleRepository, CategoryDepartmentRuleRepository>();
        services.AddScoped<ICitizenRepository, CitizenRepository>();
        services.AddScoped<IIntegrationOutboxRepository, IntegrationOutboxRepository>();
        services.AddScoped<IMunicipalityDatabaseConnectionResolver, MunicipalityDatabaseConnectionResolver>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ITrackingCodeGenerator, TrackingCodeGenerator>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();
        services.AddScoped<IAdminComplaintQueryRepository, AdminComplaintQueryRepository>();
        services.AddScoped<IPublicComplaintTrackingRepository, PublicComplaintTrackingRepository>();
        services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
