using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record CreateComplaintRequestDto(
    Guid CategoryId,
    string? Title,
    string Description,
    string? CitizenFullName,
    string? CitizenPhoneNumber,
    string? CitizenEmail,
    double Latitude,
    double Longitude,
    string? AddressText,
    bool IsAnonymous,
    ComplaintSource Source);
