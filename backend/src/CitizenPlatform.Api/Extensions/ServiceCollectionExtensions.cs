using CitizenPlatform.Api.Filters;
using CitizenPlatform.Api.HealthChecks;
using CitizenPlatform.Infrastructure;
using CitizenPlatform.Integrations;

namespace CitizenPlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCitizenPlatformApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationProblemFilter>())
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddInfrastructure(configuration);
        services.AddIntegrations();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}
