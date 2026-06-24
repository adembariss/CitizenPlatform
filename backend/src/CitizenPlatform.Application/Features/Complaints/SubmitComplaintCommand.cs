using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed record SubmitComplaintCommand(
    Guid MunicipalityId,
    string Title,
    string Description,
    double Latitude,
    double Longitude,
    SubmissionChannel Channel);
