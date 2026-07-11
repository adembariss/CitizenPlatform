namespace CitizenPlatform.Application.Abstractions;

public sealed record StatusCountRow(string Status, int Count);

public sealed record CategoryCountRow(Guid CategoryId, string CategoryName, int Count);

public sealed record DepartmentCountRow(Guid DepartmentId, string DepartmentName, int Count);

public sealed record DashboardSummaryRow(
    int TotalComplaints,
    int OpenComplaints,
    int TodayComplaints,
    int ResolvedComplaints,
    int ClosedComplaints,
    double AverageResolutionHours,
    IReadOnlyList<StatusCountRow> ByStatus,
    IReadOnlyList<CategoryCountRow> ByCategory,
    IReadOnlyList<DepartmentCountRow> ByDepartment);

public interface IAdminDashboardRepository
{
    Task<DashboardSummaryRow> GetSummaryAsync(Guid? municipalityId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
