using System.Text.Json.Serialization;
using CitizenPlatform.Api.Filters;
using CitizenPlatform.Api.HealthChecks;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Infrastructure;
using CitizenPlatform.Infrastructure.Storage;
using CitizenPlatform.Integrations;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;

namespace CitizenPlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCitizenPlatformApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationProblemFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        var maxRequestBodySizeBytes = configuration
            .GetSection(ObjectStorageOptions.SectionName)
            .GetValue<long?>(nameof(ObjectStorageOptions.MaxRequestBodySizeBytes))
            ?? 50L * 1024 * 1024;

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxRequestBodySizeBytes;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddScoped<CreateComplaintCommandHandler>();
        services.AddScoped<AddComplaintAttachmentsCommandHandler>();
        services.AddScoped<ComplaintAttachmentUploadService>();
        services.AddScoped<IValidator<CreateComplaintCommand>, CreateComplaintCommandValidator>();
        services.AddScoped<IValidator<AddComplaintAttachmentsCommand>, AddComplaintAttachmentsCommandValidator>();

        services.AddInfrastructure(configuration);
        services.AddIntegrations();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}
