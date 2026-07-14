namespace CitizenPlatform.Application.DTOs;

public sealed record PharmacyDto(
    Guid Id,
    string Name,
    string Province,
    string District,
    string? AddressText,
    string? PhoneNumber,
    double Latitude,
    double Longitude,
    bool IsOnDuty,
    // Yalnızca "en yakın" sorgusunda dolu: konuma kuş uçuşu mesafe (km).
    double? DistanceKm);
