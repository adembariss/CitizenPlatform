using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record CitizenComplaintListItemDto(
    string TrackingCode,
    string Title,
    string MunicipalityName,
    string CategoryName,
    string? DepartmentName,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CitizenProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber);
