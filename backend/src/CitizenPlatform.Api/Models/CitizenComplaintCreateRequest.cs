namespace CitizenPlatform.Api.Models;

// Authenticated citizen submission: contact details come from the account profile,
// so only the complaint content and location are supplied here.
public sealed class CitizenComplaintCreateRequest
{
    public Guid CategoryId { get; init; }

    public string? Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? AddressText { get; init; }

    // Opsiyonel: adres seçiciyle belediye doğrudan seçildiyse.
    public Guid? MunicipalityId { get; init; }
}
