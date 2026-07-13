namespace CitizenPlatform.Application.DTOs;

public sealed record PublicDistrictDto(
    Guid Id,
    string Name,
    string Code,
    double? Latitude,
    double? Longitude);
