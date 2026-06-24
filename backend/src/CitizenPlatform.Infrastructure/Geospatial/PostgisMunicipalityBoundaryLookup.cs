using CitizenPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CitizenPlatform.Infrastructure.Geospatial;

public sealed class PostgisMunicipalityBoundaryLookup : IGeoMunicipalityBoundaryLookup
{
    private readonly CitizenPlatformDbContext _dbContext;

    public PostgisMunicipalityBoundaryLookup(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MunicipalityBoundaryLookupResult?> FindContainingMunicipalityAsync(Point point, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(point);

        var longitude = point.X;
        var latitude = point.Y;

        var matchingBoundaries = _dbContext.MunicipalityBoundaries.FromSqlInterpolated($"""
            SELECT *
            FROM public.municipality_boundaries
            WHERE is_active = TRUE
              AND is_deleted = FALSE
              AND ST_Contains(
                    boundary_geometry,
                    ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)
                  )
            """);

        var query =
            from boundary in matchingBoundaries
            join municipality in _dbContext.Municipalities on boundary.MunicipalityId equals municipality.Id
            where municipality.IsActive
            select new MunicipalityBoundaryLookupResult(
                boundary.Id,
                municipality.Id,
                municipality.Name,
                municipality.Code);

        // If multiple active boundaries contain the point, pick the first deterministic match for now.
        // Future selection can order by explicit boundary priority or by the smallest containing area.
        return await query
            .OrderBy(match => match.MunicipalityName)
            .ThenBy(match => match.BoundaryId)
            .FirstOrDefaultAsync(ct);
    }
}
