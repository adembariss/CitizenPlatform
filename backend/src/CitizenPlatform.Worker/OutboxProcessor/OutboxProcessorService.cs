using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenPlatform.Worker.OutboxProcessor;

public sealed class OutboxProcessorService : BackgroundService
{
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(ILogger<OutboxProcessorService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Outbox processor heartbeat at {Timestamp}.", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
