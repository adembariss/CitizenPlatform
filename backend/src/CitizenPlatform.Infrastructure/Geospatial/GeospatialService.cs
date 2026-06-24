using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Infrastructure.Geospatial;

public sealed class GeospatialService : IGeospatialService
{
    public Task<string?> ReverseGeocodeAsync(GeoCoordinate coordinate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        return Task.FromResult<string?>(null);
    }
}
