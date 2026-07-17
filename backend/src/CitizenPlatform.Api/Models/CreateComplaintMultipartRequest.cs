using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Api.Models;

public sealed class CreateComplaintMultipartRequest
{
    public Guid CategoryId { get; init; }

    public string? Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? CitizenFullName { get; init; }

    public string? CitizenPhoneNumber { get; init; }

    public string? CitizenEmail { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? AddressText { get; init; }

    public bool IsAnonymous { get; init; }

    public ComplaintSource Source { get; init; }

    public Guid? MunicipalityId { get; init; }

    public Guid? InstitutionId { get; init; }
}
