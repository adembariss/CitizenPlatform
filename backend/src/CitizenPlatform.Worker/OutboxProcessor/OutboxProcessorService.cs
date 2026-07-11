using CitizenPlatform.Application.Features.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenPlatform.Worker.OutboxProcessor;

public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxProcessingOptions _options;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        OutboxProcessingOptions options,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessingService>();
                var processedCount = await processor.ProcessDueMessagesAsync(stoppingToken);

                if (processedCount > 0)
                {
                    _logger.LogInformation("Processed {ProcessedCount} outbox message(s).", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox processor loop failed.");
            }

            await Task.Delay(GetPollInterval(), stoppingToken);
        }
    }

    private TimeSpan GetPollInterval()
    {
        return TimeSpan.FromSeconds(_options.PollIntervalSeconds > 0
            ? _options.PollIntervalSeconds
            : 10);
    }
}
