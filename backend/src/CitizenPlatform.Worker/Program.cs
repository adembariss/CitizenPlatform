using CitizenPlatform.Infrastructure;
using CitizenPlatform.Integrations;
using CitizenPlatform.Worker.OutboxProcessor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddIntegrations();
builder.Services.AddHostedService<OutboxProcessorService>();

var host = builder.Build();
await host.RunAsync();
