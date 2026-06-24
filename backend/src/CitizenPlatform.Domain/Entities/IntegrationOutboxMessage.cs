using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

public sealed class IntegrationOutboxMessage : AuditableEntity
{
    private readonly List<IntegrationAttempt> _attempts = [];

    private IntegrationOutboxMessage()
    {
    }

    private IntegrationOutboxMessage(
        Guid id,
        Guid municipalityId,
        Guid aggregateId,
        string aggregateType,
        string messageType,
        string payload)
        : base(id)
    {
        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        AggregateId = Guard.AgainstEmpty(aggregateId, nameof(aggregateId));
        AggregateType = Guard.AgainstEmpty(aggregateType, nameof(aggregateType), 100);
        MessageType = Guard.AgainstEmpty(messageType, nameof(messageType), 200);
        Payload = Guard.AgainstEmpty(payload, nameof(payload), 100_000);
        Status = OutboxStatus.Pending;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid MunicipalityId { get; private set; }

    public Guid AggregateId { get; private set; }

    public string AggregateType { get; private set; } = string.Empty;

    public string MessageType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public OutboxStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessingStartedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? NextRetryAt { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<IntegrationAttempt> Attempts => _attempts.AsReadOnly();

    public static IntegrationOutboxMessage Create(
        Guid municipalityId,
        Guid aggregateId,
        string aggregateType,
        string messageType,
        string payload)
    {
        return new IntegrationOutboxMessage(Guid.NewGuid(), municipalityId, aggregateId, aggregateType, messageType, payload);
    }

    public IntegrationAttempt MarkProcessing()
    {
        if (Status is OutboxStatus.Completed or OutboxStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot process outbox message in {Status} status.");
        }

        Status = OutboxStatus.Processing;
        AttemptCount++;
        ProcessingStartedAt = DateTimeOffset.UtcNow;
        FailureReason = null;
        Touch();

        var attempt = IntegrationAttempt.Start(Id, AttemptCount);
        _attempts.Add(attempt);

        return attempt;
    }

    public void MarkCompleted()
    {
        Status = OutboxStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
        NextRetryAt = null;
        FailureReason = null;
        Touch();
    }

    public void MarkFailed(string failureReason, DateTimeOffset? nextRetryAt)
    {
        Status = OutboxStatus.Failed;
        FailureReason = Guard.AgainstEmpty(failureReason, nameof(failureReason), 4000);
        NextRetryAt = nextRetryAt;
        Touch();
    }

    public void Cancel(string reason)
    {
        Status = OutboxStatus.Cancelled;
        FailureReason = Guard.AgainstEmpty(reason, nameof(reason), 4000);
        NextRetryAt = null;
        Touch();
    }
}
