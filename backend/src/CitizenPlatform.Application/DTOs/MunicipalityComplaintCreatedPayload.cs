namespace CitizenPlatform.Application.DTOs;

public sealed record MunicipalityComplaintCreatedPayload(
    Guid ComplaintId,
    Guid MunicipalityId,
    string TrackingCode,
    string CategoryName,
    string? DepartmentName,
    string? CitizenFullName,
    string? CitizenPhoneNumber,
    string? CitizenEmail,
    string Description,
    string? AddressText,
    double Latitude,
    double Longitude,
    string Status,
    string Priority,
    DateTimeOffset CreatedAt);
