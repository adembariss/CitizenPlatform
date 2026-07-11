using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class AdminDashboardRepository : IAdminDashboardRepository
{
    private static readonly ComplaintStatus[] OpenStatuses =
    [
        ComplaintStatus.New,
        ComplaintStatus.UnderReview,
        ComplaintStatus.Assigned,
        ComplaintStatus.InProgress,
        ComplaintStatus.WaitingForCitizen
    ];

    private readonly CitizenPlatformDbContext _dbContext;

    public AdminDashboardRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryRow> GetSummaryAsync(
        Guid? municipalityId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var complaints = _dbContext.Complaints.AsQueryable();
        if (municipalityId is not null)
        {
            complaints = complaints.Where(complaint => complaint.MunicipalityId == municipalityId);
        }

        var totalComplaints = await complaints.CountAsync(cancellationToken);
        var openComplaints = await complaints.CountAsync(complaint => OpenStatuses.Contains(complaint.Status), cancellationToken);

        var todayStart = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);
        var todayComplaints = await complaints.CountAsync(
            complaint => complaint.CreatedAt >= todayStart && complaint.CreatedAt < todayEnd,
            cancellationToken);

        var resolvedComplaints = await complaints.CountAsync(complaint => complaint.Status == ComplaintStatus.Resolved, cancellationToken);
        var closedComplaints = await complaints.CountAsync(complaint => complaint.Status == ComplaintStatus.Closed, cancellationToken);

        var closedDurations = await complaints
            .Where(complaint => complaint.ClosedAt != null)
            .Select(complaint => new { complaint.CreatedAt, ClosedAt = complaint.ClosedAt!.Value })
            .ToListAsync(cancellationToken);

        var averageResolutionHours = closedDurations.Count == 0
            ? 0d
            : closedDurations.Average(duration => (duration.ClosedAt - duration.CreatedAt).TotalHours);

        var byStatusRaw = await complaints
            .GroupBy(complaint => complaint.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var byStatus = byStatusRaw
            .Select(row => new StatusCountRow(row.Status.ToString(), row.Count))
            .ToArray();

        var byCategoryRaw = await complaints
            .GroupBy(complaint => complaint.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var categoryIds = byCategoryRaw.Select(row => row.CategoryId).ToArray();
        var categoryNames = await _dbContext.ComplaintCategories
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);
        var byCategory = byCategoryRaw
            .Select(row => new CategoryCountRow(row.CategoryId, categoryNames.GetValueOrDefault(row.CategoryId, string.Empty), row.Count))
            .ToArray();

        var byDepartmentRaw = await complaints
            .Where(complaint => complaint.CurrentDepartmentId != null)
            .GroupBy(complaint => complaint.CurrentDepartmentId!.Value)
            .Select(group => new { DepartmentId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var departmentIds = byDepartmentRaw.Select(row => row.DepartmentId).ToArray();
        var departmentNames = await _dbContext.Departments
            .Where(department => departmentIds.Contains(department.Id))
            .ToDictionaryAsync(department => department.Id, department => department.Name, cancellationToken);
        var byDepartment = byDepartmentRaw
            .Select(row => new DepartmentCountRow(row.DepartmentId, departmentNames.GetValueOrDefault(row.DepartmentId, string.Empty), row.Count))
            .ToArray();

        return new DashboardSummaryRow(
            totalComplaints,
            openComplaints,
            todayComplaints,
            resolvedComplaints,
            closedComplaints,
            averageResolutionHours,
            byStatus,
            byCategory,
            byDepartment);
    }
}
