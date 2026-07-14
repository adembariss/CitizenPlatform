using CitizenPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CitizenPlatform.Infrastructure.Notifications;

/// <summary>
/// Development SMS sender: logs the message instead of sending. Since no real gateway
/// is wired yet, it also reveals the code so the demo UI can display it.
/// Replace with a real provider (Netgsm/Twilio/…) in production; set RevealsCode=false.
/// </summary>
public sealed class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public bool RevealsCode => true;

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SMS] -> {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
