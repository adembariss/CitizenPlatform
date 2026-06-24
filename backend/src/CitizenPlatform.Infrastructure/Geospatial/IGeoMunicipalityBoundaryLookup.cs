using NetTopologySuite.Geometries;

namespace CitizenPlatform.Infrastructure.Geospatial;

public interface IGeoMunicipalityBoundaryLookup
{
    Task<MunicipalityBoundaryLookupResult?> FindContainingMunicipalityAsync(Point point, CancellationToken ct);
}
