using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed record SubmitComplaintCommand(
    Guid CategoryId,
    string Title,
    string Description,
    double Latitude,
    double Longitude,
    ComplaintSource Source,
    Guid? CitizenId = null,
    double? PhotoExifLatitude = null,
    double? PhotoExifLongitude = null);
