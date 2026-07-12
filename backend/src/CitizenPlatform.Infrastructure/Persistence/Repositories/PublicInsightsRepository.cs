using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class PublicInsightsRepository : IPublicInsightsRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public PublicInsightsRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PublicStatsRow> GetStatsAsync(CancellationToken cancellationToken)
    {
        var total = await _dbContext.Complaints.CountAsync(cancellationToken);

        var resolved = await _dbContext.Complaints
            .CountAsync(
                complaint => complaint.Status == ComplaintStatus.Resolved || complaint.Status == ComplaintStatus.Closed,
                cancellationToken);

        var municipalities = await _dbContext.Municipalities.CountAsync(m => m.IsActive, cancellationToken);

        var categories = await _dbContext.ComplaintCategories.CountAsync(category => category.IsActive, cancellationToken);

        return new PublicStatsRow(total, resolved, municipalities, categories);
    }

    public async Task<IReadOnlyList<PublicComplaintMapPointRow>> GetMapPointsAsync(
        Guid? municipalityId,
        int limit,
        CancellationToken cancellationToken)
    {
        var query =
            from complaint in _dbContext.Complaints.AsNoTracking()
            join category in _dbContext.ComplaintCategories on complaint.CategoryId equals category.Id
            select new { complaint, category };

        if (municipalityId is not null)
        {
            query = query.Where(row => row.complaint.MunicipalityId == municipalityId);
        }

        return await query
            .OrderByDescending(row => row.complaint.CreatedAt)
            .Take(limit)
            .Select(row => new PublicComplaintMapPointRow(
                row.complaint.Location.Latitude,
                row.complaint.Location.Longitude,
                row.category.Name,
                row.complaint.Status,
                row.complaint.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
