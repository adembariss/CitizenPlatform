namespace CitizenPlatform.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
