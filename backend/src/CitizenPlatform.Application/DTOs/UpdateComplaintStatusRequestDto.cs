using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record UpdateComplaintStatusRequestDto(
    ComplaintStatus NewStatus,
    string? Note,
    bool IsVisibleToCitizen = true);

public sealed record AssignComplaintRequestDto(
    Guid DepartmentId,
    Guid? AssignedUserId,
    string? Note);

public sealed record AddComplaintCommentRequestDto(
    string CommentText,
    bool IsInternal);
