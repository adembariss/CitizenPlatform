using System.Text.Json.Serialization;
using CitizenPlatform.Api.Filters;
using CitizenPlatform.Api.HealthChecks;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Infrastructure;
using CitizenPlatform.Integrations;
using FluentValidation;

namespace CitizenPlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCitizenPlatformApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationProblemFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddScoped<CreateComplaintCommandHandler>();
        services.AddScoped<IValidator<CreateComplaintCommand>, CreateComplaintCommandValidator>();

        services.AddInfrastructure(configuration);
        services.AddIntegrations();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}
