using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record CreateComplaintResponseDto(
    Guid ComplaintId,
    string TrackingCode,
    string MunicipalityName,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt);
