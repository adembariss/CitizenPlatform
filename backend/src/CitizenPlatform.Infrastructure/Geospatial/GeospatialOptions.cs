namespace CitizenPlatform.Infrastructure.Geospatial;

public sealed class GeospatialOptions
{
    public const string SectionName = "Geospatial";

    public string Provider { get; init; } = "PostGIS";
}
