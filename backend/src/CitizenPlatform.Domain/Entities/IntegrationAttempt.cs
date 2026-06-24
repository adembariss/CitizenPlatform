using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class IntegrationAttempt : AuditableEntity
{
    private IntegrationAttempt()
    {
    }

    private IntegrationAttempt(Guid id, Guid outboxMessageId, int attemptNumber)
        : base(id)
    {
        OutboxMessageId = Guard.AgainstEmpty(outboxMessageId, nameof(outboxMessageId));
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive.");
        }

        AttemptNumber = attemptNumber;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public Guid OutboxMessageId { get; private set; }

    public int AttemptNumber { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool? Succeeded { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static IntegrationAttempt Start(Guid outboxMessageId, int attemptNumber)
    {
        return new IntegrationAttempt(Guid.NewGuid(), outboxMessageId, attemptNumber);
    }

    public void Complete()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Succeeded = true;
        ErrorMessage = null;
        Touch();
    }

    public void Fail(string errorMessage)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Succeeded = false;
        ErrorMessage = Guard.AgainstEmpty(errorMessage, nameof(errorMessage), 4000);
        Touch();
    }
}
