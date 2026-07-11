using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record AdminComplaintListItemDto(
    Guid Id,
    string TrackingCode,
    string MunicipalityName,
    string CategoryName,
    string? DepartmentName,
    string Title,
    string DescriptionSummary,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    string? CitizenFullName,
    string? AddressText,
    double Latitude,
    double Longitude,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AdminComplaintListResponseDto(
    IReadOnlyList<AdminComplaintListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
