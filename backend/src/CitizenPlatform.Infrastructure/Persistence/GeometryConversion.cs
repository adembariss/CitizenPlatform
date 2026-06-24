using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace CitizenPlatform.Infrastructure.Persistence;

internal static class GeometryConversion
{
    private static readonly NtsGeometryServices GeometryServices = NtsGeometryServices.Instance;

    public static Point ToPoint(string wkt)
    {
        return ReadGeometry<Point>(wkt);
    }

    public static Point? ToNullablePoint(string? wkt)
    {
        return string.IsNullOrWhiteSpace(wkt) ? null : ReadGeometry<Point>(wkt);
    }

    public static MultiPolygon ToMultiPolygon(string wkt)
    {
        return ReadGeometry<MultiPolygon>(wkt);
    }

    public static string ToWkt(Geometry geometry)
    {
        geometry.SRID = 4326;
        return geometry.AsText();
    }

    public static string? ToNullableWkt(Geometry? geometry)
    {
        return geometry is null ? null : ToWkt(geometry);
    }

    private static TGeometry ReadGeometry<TGeometry>(string wkt)
        where TGeometry : Geometry
    {
        var reader = new WKTReader(GeometryServices);
        var geometry = reader.Read(wkt);
        geometry.SRID = 4326;

        if (geometry is not TGeometry typedGeometry)
        {
            throw new InvalidOperationException($"Expected {typeof(TGeometry).Name} geometry but received {geometry.GeometryType}.");
        }

        return typedGeometry;
    }
}
