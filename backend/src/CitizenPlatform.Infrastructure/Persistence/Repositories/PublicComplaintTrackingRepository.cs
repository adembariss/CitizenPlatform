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

        // Şikayet bir dağıtım kurumuna (elektrik/su/doğalgaz) düştüyse kurum adını göster;
        // aksi halde konum belediyesinin adı.
        string organizationName;
        if (complaint.InstitutionId is Guid institutionId)
        {
            var institution = await _dbContext.Institutions
                .AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == institutionId, cancellationToken);
            organizationName = institution?.Name ?? string.Empty;
        }
        else
        {
            var municipality = await _dbContext.Municipalities
                .AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == complaint.MunicipalityId, cancellationToken);
            organizationName = municipality?.Name ?? string.Empty;
        }

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
            organizationName,
            category?.Name ?? string.Empty,
            departmentName);
    }
}
