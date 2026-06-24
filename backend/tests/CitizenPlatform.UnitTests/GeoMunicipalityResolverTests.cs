using CitizenPlatform.Infrastructure.Geospatial;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class GeoMunicipalityResolverTests
{
    private static readonly Guid DemoBoundaryId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    private static readonly Guid DemoMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task ResolveByCoordinateAsync_WhenCoordinateIsInsideDemoBoundary_ReturnsMunicipality()
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveByCoordinateAsync(41.05, 29.00, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DemoMunicipalityId, result.MunicipalityId);
        Assert.Equal("Demo Belediyesi", result.MunicipalityName);
        Assert.Equal("DEMO", result.MunicipalityCode);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task ResolveByCoordinateAsync_WhenCoordinateIsOutsideBoundary_ReturnsFailure()
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveByCoordinateAsync(39.90, 32.85, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.MunicipalityId);
        Assert.Contains("No active municipality boundary", result.FailureReason);
    }

    [Fact]
    public async Task ResolveByCoordinateAsync_WhenCoordinateIsInvalid_ReturnsValidationFailure()
    {
        var lookup = new DemoBoundaryLookup();
        var resolver = CreateResolver(lookup);

        var result = await resolver.ResolveByCoordinateAsync(91, 29, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid coordinate", result.FailureReason);
        Assert.False(lookup.WasCalled);
    }

    private static GeoMunicipalityResolver CreateResolver(DemoBoundaryLookup? lookup = null)
    {
        return new GeoMunicipalityResolver(
            lookup ?? new DemoBoundaryLookup(),
            NullLogger<GeoMunicipalityResolver>.Instance);
    }

    private sealed class DemoBoundaryLookup : IGeoMunicipalityBoundaryLookup
    {
        public bool WasCalled { get; private set; }

        public Task<MunicipalityBoundaryLookupResult?> FindContainingMunicipalityAsync(Point point, CancellationToken ct)
        {
            WasCalled = true;

            var isInsideDemoBoundary = point.Y is >= 40.95 and <= 41.20
                && point.X is >= 28.85 and <= 29.20;

            MunicipalityBoundaryLookupResult? result = isInsideDemoBoundary
                ? new MunicipalityBoundaryLookupResult(DemoBoundaryId, DemoMunicipalityId, "Demo Belediyesi", "DEMO")
                : null;

            return Task.FromResult(result);
        }
    }
}
