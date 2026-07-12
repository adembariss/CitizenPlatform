using CitizenPlatform.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class PublicComplaintTrackingRepository : IPublicComplaintTrackingRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public PublicComplaintTrackingRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PublicComplaintTrackingRow?> GetByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken)
    {
        var complaint = await _dbContext.Complaints
            .AsNoTracking()
            .Include(entity => entity.Attachments)
            .Include(entity => entity.StatusHistories)
            .Include(entity => entity.Comments)
            .FirstOrDefaultAsync(entity => entity.TrackingCode == trackingCode, cancellationToken);

        if (complaint is null)
        {
            return null;
        }

        var municipality = await _dbContext.Municipalities
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == complaint.MunicipalityId, cancellationToken);

        var category = await _dbContext.ComplaintCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == complaint.CategoryId, cancellationToken);

        var departmentName = complaint.CurrentDepartmentId is null
            ? null
            : await _dbContext.Departments
                .AsNoTracking()
                .Where(department => department.Id == complaint.CurrentDepartmentId)
                .Select(department => department.Name)
                .FirstOrDefaultAsync(cancellationToken);

        return new PublicComplaintTrackingRow(
            complaint,
            municipality?.Name ?? string.Empty,
            category?.Name ?? string.Empty,
            departmentName);
    }
}
