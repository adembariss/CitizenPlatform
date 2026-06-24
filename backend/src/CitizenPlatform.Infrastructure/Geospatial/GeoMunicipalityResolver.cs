using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace CitizenPlatform.Infrastructure.Geospatial;

public sealed class GeoMunicipalityResolver : IGeoMunicipalityResolver
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    private readonly IGeoMunicipalityBoundaryLookup _boundaryLookup;
    private readonly ILogger<GeoMunicipalityResolver> _logger;

    public GeoMunicipalityResolver(
        IGeoMunicipalityBoundaryLookup boundaryLookup,
        ILogger<GeoMunicipalityResolver> logger)
    {
        _boundaryLookup = boundaryLookup;
        _logger = logger;
    }

    public async Task<MunicipalityResolveResult> ResolveByCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken ct)
    {
        GeoCoordinate coordinate;

        try
        {
            coordinate = new GeoCoordinate(latitude, longitude);
        }
        catch (ArgumentException exception)
        {
            return MunicipalityResolveResult.Failure($"Invalid coordinate: {exception.Message}");
        }

        var point = GeometryFactory.CreatePoint(new Coordinate(coordinate.Longitude, coordinate.Latitude));
        point.SRID = 4326;

        try
        {
            var match = await _boundaryLookup.FindContainingMunicipalityAsync(point, ct);
            if (match is null)
            {
                return MunicipalityResolveResult.Failure("No active municipality boundary contains the coordinate.");
            }

            return MunicipalityResolveResult.Success(
                match.MunicipalityId,
                match.MunicipalityName,
                match.MunicipalityCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to resolve municipality for coordinate {Latitude}, {Longitude}.",
                latitude,
                longitude);

            return MunicipalityResolveResult.Failure("Municipality resolution failed.");
        }
    }
}
