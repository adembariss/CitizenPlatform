using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class CitizenComplaintRepository : ICitizenComplaintRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public CitizenComplaintRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CitizenComplaintListItemDto>> ListByCitizenAsync(
        Guid citizenId,
        CancellationToken cancellationToken)
    {
        var query =
            from complaint in _dbContext.Complaints.AsNoTracking()
            where complaint.CitizenId == citizenId
            join municipality in _dbContext.Municipalities on complaint.MunicipalityId equals municipality.Id
            join category in _dbContext.ComplaintCategories on complaint.CategoryId equals category.Id
            join department in _dbContext.Departments
                on complaint.CurrentDepartmentId equals (Guid?)department.Id into departmentJoin
            from department in departmentJoin.DefaultIfEmpty()
            orderby complaint.CreatedAt descending
            select new CitizenComplaintListItemDto(
                complaint.TrackingCode,
                complaint.Title,
                municipality.Name,
                category.Name,
                department != null ? department.Name : null,
                complaint.Status,
                complaint.CreatedAt,
                complaint.UpdatedAt);

        return await query.ToListAsync(cancellationToken);
    }
}
