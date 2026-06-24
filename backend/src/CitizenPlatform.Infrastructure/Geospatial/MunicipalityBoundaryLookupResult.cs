namespace CitizenPlatform.Infrastructure.Geospatial;

public sealed record MunicipalityBoundaryLookupResult(
    Guid BoundaryId,
    Guid MunicipalityId,
    string MunicipalityName,
    string MunicipalityCode);
