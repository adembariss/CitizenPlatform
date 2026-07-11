using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record AdminComplaintAttachmentDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    string Sha256Hash,
    DateTimeOffset CreatedAt);

public sealed record AdminComplaintStatusHistoryDto(
    Guid Id,
    ComplaintStatus? PreviousStatus,
    ComplaintStatus NewStatus,
    Guid? ChangedByUserId,
    string? Note,
    bool IsVisibleToCitizen,
    DateTimeOffset CreatedAt);

public sealed record AdminComplaintCommentDto(
    Guid Id,
    Guid AuthorUserId,
    string Body,
    bool IsInternal,
    DateTimeOffset CreatedAt);

public sealed record AdminComplaintAssignmentDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid AssignedByUserId,
    Guid? AssignedUserId,
    string? Note,
    DateTimeOffset AssignedAt);

public sealed record AdminComplaintDetailDto(
    Guid Id,
    string TrackingCode,
    Guid MunicipalityId,
    string MunicipalityName,
    Guid CategoryId,
    string CategoryName,
    Guid? DepartmentId,
    string? DepartmentName,
    string Title,
    string Description,
    string? CitizenFullName,
    string? CitizenPhoneNumber,
    string? CitizenEmail,
    string? AddressText,
    double Latitude,
    double Longitude,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    ComplaintSource Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<AdminComplaintAttachmentDto> Attachments,
    IReadOnlyList<AdminComplaintStatusHistoryDto> StatusHistories,
    IReadOnlyList<AdminComplaintCommentDto> Comments,
    IReadOnlyList<AdminComplaintAssignmentDto> Assignments);

public sealed record AdminComplaintHistoryDto(
    Guid ComplaintId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<AdminComplaintStatusHistoryDto> StatusHistories,
    IReadOnlyList<AdminComplaintCommentDto> Comments,
    IReadOnlyList<AdminComplaintAssignmentDto> Assignments);
