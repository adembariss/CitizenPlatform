using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record PublicComplaintTrackingDto(
    string TrackingCode,
    string MunicipalityName,
    string CategoryName,
    string? DepartmentName,
    string Title,
    string Description,
    string? AddressText,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ClosedAt,
    int AttachmentCount,
    IReadOnlyList<PublicComplaintStatusHistoryDto> StatusHistory,
    IReadOnlyList<PublicComplaintResponseDto> Responses,
    // true ise şikayet bir dağıtım kurumuna (elektrik/su/doğalgaz) düştü; MunicipalityName kurum adını taşır.
    bool IsInstitution);

public sealed record PublicComplaintStatusHistoryDto(
    ComplaintStatus? PreviousStatus,
    ComplaintStatus NewStatus,
    string? Note,
    DateTimeOffset CreatedAt);

public sealed record PublicComplaintResponseDto(
    string Body,
    DateTimeOffset CreatedAt);
