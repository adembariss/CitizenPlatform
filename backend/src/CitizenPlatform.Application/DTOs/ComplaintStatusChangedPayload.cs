namespace CitizenPlatform.Application.DTOs;

public sealed record ComplaintStatusChangedPayload(
    Guid ComplaintId,
    Guid MunicipalityId,
    string TrackingCode,
    string? OldStatus,
    string NewStatus,
    string? Note,
    bool IsVisibleToCitizen,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAt);

public sealed record ComplaintAssignedPayload(
    Guid ComplaintId,
    Guid MunicipalityId,
    string TrackingCode,
    Guid DepartmentId,
    string DepartmentName,
    Guid? AssignedUserId,
    string? Note,
    Guid AssignedByUserId,
    DateTimeOffset AssignedAt);

public sealed record AdminCommentAddedPayload(
    Guid ComplaintId,
    Guid MunicipalityId,
    string TrackingCode,
    string CommentText,
    bool IsInternal,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);
