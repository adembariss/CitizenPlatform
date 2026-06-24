using CitizenPlatform.Api.Filters;
using CitizenPlatform.Infrastructure;
using CitizenPlatform.Integrations;

namespace CitizenPlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCitizenPlatformApi(this IServiceCollection services)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationProblemFilter>())
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        services.AddInfrastructure();
        services.AddIntegrations();

        return services;
    }
}
