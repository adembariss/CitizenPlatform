namespace CitizenPlatform.Integrations.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Type { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedOn { get; private set; }

    public void MarkProcessed(DateTimeOffset processedOn)
    {
        ProcessedOn = processedOn;
    }
}
