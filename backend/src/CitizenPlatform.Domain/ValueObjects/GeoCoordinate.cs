using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.ValueObjects;

public sealed class GeoCoordinate : ValueObject
{
    private GeoCoordinate()
    {
    }

    public GeoCoordinate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public string ToWktPoint()
    {
        return FormattableString.Invariant($"POINT ({Longitude} {Latitude})");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
