using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Integrations.MunicipalityDb;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenPlatform.Integrations;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddScoped<IMunicipalityComplaintWriter, PostgreSqlMunicipalityComplaintWriter>();
        return services;
    }
}
