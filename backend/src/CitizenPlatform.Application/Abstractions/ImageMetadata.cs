using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Application.Abstractions;

public sealed record ImageMetadata(
    GeoCoordinate? GpsLocation,
    DateTimeOffset? TakenAt)
{
    public static ImageMetadata Empty { get; } = new(null, null);
}
