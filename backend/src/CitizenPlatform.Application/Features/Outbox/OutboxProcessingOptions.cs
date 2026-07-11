namespace CitizenPlatform.Application.Features.Outbox;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "OutboxProcessor";

    public int BatchSize { get; init; } = 20;

    public int PollIntervalSeconds { get; init; } = 10;

    public int MaxRetryCount { get; init; } = 5;

    public int InitialRetryDelaySeconds { get; init; } = 30;

    public int MaxRetryDelaySeconds { get; init; } = 15 * 60;
}
