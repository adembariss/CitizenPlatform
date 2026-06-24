using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Events;

public sealed record ComplaintStatusChangedDomainEvent(
    Guid ComplaintId,
    ComplaintStatus PreviousStatus,
    ComplaintStatus NewStatus,
    Guid ChangedByUserId,
    DateTimeOffset OccurredOn) : IDomainEvent;
