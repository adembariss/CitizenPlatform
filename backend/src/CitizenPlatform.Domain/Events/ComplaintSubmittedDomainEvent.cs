using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Events;

public sealed record ComplaintSubmittedDomainEvent(
    Guid ComplaintId,
    Guid MunicipalityId,
    DateTimeOffset OccurredOn) : IDomainEvent;
