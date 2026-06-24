using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class GeoCoordinateTests
{
    [Fact]
    public void Constructor_WhenLatitudeIsOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(91, 29));
    }

    [Fact]
    public void Constructor_WhenLongitudeIsOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(41, 181));
    }

    [Fact]
    public void ToWktPoint_ReturnsLongitudeLatitudeOrder()
    {
        var coordinate = new GeoCoordinate(41.0082, 28.9784);

        Assert.Equal("POINT (28.9784 41.0082)", coordinate.ToWktPoint());
    }
}
