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
}
