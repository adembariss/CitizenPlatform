using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record ComplaintDto(
    Guid Id,
    Guid MunicipalityId,
    string Title,
    string Description,
    double Latitude,
    double Longitude,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    DateTimeOffset CreatedAt);
