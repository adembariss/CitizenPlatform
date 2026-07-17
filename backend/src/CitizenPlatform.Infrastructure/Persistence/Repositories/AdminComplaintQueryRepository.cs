using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class AdminComplaintQueryRepository : IAdminComplaintQueryRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public AdminComplaintQueryRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminComplaintPagedRows> SearchAsync(
        AdminComplaintSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query =
            from complaint in _dbContext.Complaints
            join municipality in _dbContext.Municipalities on complaint.MunicipalityId equals municipality.Id
            join category in _dbContext.ComplaintCategories on complaint.CategoryId equals category.Id
            join department in _dbContext.Departments
                on complaint.CurrentDepartmentId equals (Guid?)department.Id into departmentJoin
            from department in departmentJoin.DefaultIfEmpty()
            join citizen in _dbContext.Citizens
                on complaint.CitizenId equals (Guid?)citizen.Id into citizenJoin
            from citizen in citizenJoin.DefaultIfEmpty()
            select new { complaint, municipality, category, department, citizen };

        if (criteria.InstitutionId is not null)
        {
            // Kurum yöneticisi: yalnızca o kuruma düşen şikayetler.
            query = query.Where(row => row.complaint.InstitutionId == criteria.InstitutionId);
        }
        else
        {
            // Belediye/SystemAdmin görünümü: kuruma (elektrik/su/doğalgaz) düşenler hariç.
            query = query.Where(row => row.complaint.InstitutionId == null);

            if (criteria.MunicipalityId is not null)
            {
                query = query.Where(row => row.complaint.MunicipalityId == criteria.MunicipalityId);
            }
        }

        if (!string.IsNullOrWhiteSpace(criteria.Province))
        {
            query = query.Where(row => row.municipality.Province == criteria.Province);
        }

        if (criteria.Status is not null)
        {
            query = query.Where(row => row.complaint.Status == criteria.Status);
        }

        if (criteria.CategoryId is not null)
        {
            query = query.Where(row => row.complaint.CategoryId == criteria.CategoryId);
        }

        if (criteria.DepartmentId is not null)
        {
            query = query.Where(row => row.complaint.CurrentDepartmentId == criteria.DepartmentId);
        }

        if (criteria.DateFrom is not null)
        {
            query = query.Where(row => row.complaint.CreatedAt >= criteria.DateFrom);
        }

        if (criteria.DateTo is not null)
        {
            query = query.Where(row => row.complaint.CreatedAt <= criteria.DateTo);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim().ToLowerInvariant();
            query = query.Where(row =>
                row.complaint.TrackingCode.ToLower().Contains(term)
                || row.complaint.Title.ToLower().Contains(term)
                || row.complaint.Description.ToLower().Contains(term)
                || (row.complaint.AddressText != null && row.complaint.AddressText.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(row => row.complaint.CreatedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(row => new AdminComplaintListRow(
                row.complaint.Id,
                row.complaint.TrackingCode,
                row.municipality.Name,
                row.category.Name,
                row.department != null ? row.department.Name : null,
                row.complaint.Title,
                row.complaint.Description,
                row.complaint.Status,
                row.complaint.Priority,
                row.citizen != null ? row.citizen.FullName : null,
                row.complaint.AddressText,
                row.complaint.Location.Latitude,
                row.complaint.Location.Longitude,
                row.complaint.CreatedAt,
                row.complaint.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new AdminComplaintPagedRows(items, criteria.Page, criteria.PageSize, totalCount);
    }

    public async Task<AdminComplaintDetailRow?> GetDetailAsync(
        Guid complaintId,
        Guid? tenantMunicipalityId,
        CancellationToken cancellationToken)
    {
        var complaint = await _dbContext.Complaints
            .Include(entity => entity.Attachments)
            .Include(entity => entity.StatusHistories)
            .Include(entity => entity.Comments)
            .Include(entity => entity.Assignments)
            .FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken);

        if (complaint is null || (tenantMunicipalityId is not null && complaint.MunicipalityId != tenantMunicipalityId))
        {
            return null;
        }

        var municipality = await _dbContext.Municipalities
            .FirstOrDefaultAsync(entity => entity.Id == complaint.MunicipalityId, cancellationToken);

        var category = await _dbContext.ComplaintCategories
            .FirstOrDefaultAsync(entity => entity.Id == complaint.CategoryId, cancellationToken);

        Citizen? citizen = complaint.CitizenId is null
            ? null
            : await _dbContext.Citizens.FirstOrDefaultAsync(entity => entity.Id == complaint.CitizenId, cancellationToken);

        var departmentIds = complaint.Assignments
            .Select(assignment => assignment.DepartmentId)
            .Concat(complaint.CurrentDepartmentId is null
                ? Array.Empty<Guid>()
                : new[] { complaint.CurrentDepartmentId.Value })
            .Distinct()
            .ToArray();

        var departmentNamesById = departmentIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Departments
                .Where(department => departmentIds.Contains(department.Id))
                .ToDictionaryAsync(department => department.Id, department => department.Name, cancellationToken);

        var currentDepartmentName = complaint.CurrentDepartmentId is not null
            ? departmentNamesById.GetValueOrDefault(complaint.CurrentDepartmentId.Value)
            : null;

        return new AdminComplaintDetailRow(
            complaint,
            municipality?.Name ?? string.Empty,
            category?.Name ?? string.Empty,
            currentDepartmentName,
            citizen?.FullName,
            citizen?.PhoneNumber,
            citizen?.Email,
            departmentNamesById);
    }
}
