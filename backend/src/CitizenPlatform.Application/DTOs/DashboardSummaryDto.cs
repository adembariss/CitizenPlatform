namespace CitizenPlatform.Application.DTOs;

public sealed record DashboardStatusCountDto(string Status, int Count);

public sealed record DashboardCategoryCountDto(Guid CategoryId, string CategoryName, int Count);

public sealed record DashboardDepartmentCountDto(Guid DepartmentId, string DepartmentName, int Count);

public sealed record DashboardSummaryDto(
    int TotalComplaints,
    int OpenComplaints,
    int TodayComplaints,
    int ResolvedComplaints,
    int ClosedComplaints,
    double AverageResolutionHours,
    IReadOnlyList<DashboardStatusCountDto> ByStatus,
    IReadOnlyList<DashboardCategoryCountDto> ByCategory,
    IReadOnlyList<DashboardDepartmentCountDto> ByDepartment);

public sealed record MunicipalityMapContextDto(
    Guid MunicipalityId,
    string MunicipalityName,
    double? CenterLatitude,
    double? CenterLongitude,
    string? BoundaryGeoJson);
