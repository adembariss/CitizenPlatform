using CitizenPlatform.Infrastructure;
using CitizenPlatform.Integrations;
using CitizenPlatform.Application.Features.Outbox;
using CitizenPlatform.Worker.OutboxProcessor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations();
builder.Services.AddScoped<OutboxProcessingService>();
builder.Services.AddSingleton(CreateOutboxProcessingOptions(builder.Configuration));
builder.Services.AddHostedService<OutboxProcessorService>();

var host = builder.Build();
await host.RunAsync();

static OutboxProcessingOptions CreateOutboxProcessingOptions(IConfiguration configuration)
{
    var section = configuration.GetSection(OutboxProcessingOptions.SectionName);

    return new OutboxProcessingOptions
    {
        BatchSize = ReadInt(section, nameof(OutboxProcessingOptions.BatchSize), 20),
        PollIntervalSeconds = ReadInt(section, nameof(OutboxProcessingOptions.PollIntervalSeconds), 10),
        MaxRetryCount = ReadInt(section, nameof(OutboxProcessingOptions.MaxRetryCount), 5),
        InitialRetryDelaySeconds = ReadInt(section, nameof(OutboxProcessingOptions.InitialRetryDelaySeconds), 30),
        MaxRetryDelaySeconds = ReadInt(section, nameof(OutboxProcessingOptions.MaxRetryDelaySeconds), 900)
    };
}

static int ReadInt(IConfiguration configuration, string key, int fallback)
{
    return int.TryParse(configuration[key], out var value) && value > 0
        ? value
        : fallback;
}
