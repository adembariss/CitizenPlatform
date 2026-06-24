using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Application.Abstractions;

public interface IGeospatialService
{
    Task<string?> ReverseGeocodeAsync(GeoCoordinate coordinate, CancellationToken cancellationToken);
}
