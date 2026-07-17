using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.AdminDashboard;

public sealed class DashboardSummaryQueryHandler
{
    private readonly IAdminDashboardRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DashboardSummaryQueryHandler(IAdminDashboardRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> HandleAsync(
        Guid? requestedMunicipalityId,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var municipalityId = scope.ResolveListFilter(requestedMunicipalityId);
        var row = await _repository.GetSummaryAsync(municipalityId, scope.InstitutionId, _dateTimeProvider.UtcNow, cancellationToken);

        return new DashboardSummaryDto(
            row.TotalComplaints,
            row.OpenComplaints,
            row.TodayComplaints,
            row.ResolvedComplaints,
            row.ClosedComplaints,
            row.AverageResolutionHours,
            row.ByStatus.Select(status => new DashboardStatusCountDto(status.Status, status.Count)).ToArray(),
            row.ByCategory.Select(category => new DashboardCategoryCountDto(category.CategoryId, category.CategoryName, category.Count)).ToArray(),
            row.ByDepartment.Select(department => new DashboardDepartmentCountDto(department.DepartmentId, department.DepartmentName, department.Count)).ToArray());
    }
}

public sealed class MunicipalityMapContextQueryHandler
{
    private readonly IAdminDashboardRepository _repository;

    public MunicipalityMapContextQueryHandler(IAdminDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<MunicipalityMapContextDto?> HandleAsync(TenantScope scope, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorizedAdmin || scope.MunicipalityId is not Guid municipalityId)
        {
            return null;
        }

        var row = await _repository.GetMapContextAsync(municipalityId, cancellationToken);
        return row is null
            ? null
            : new MunicipalityMapContextDto(
                row.MunicipalityId,
                row.MunicipalityName,
                row.CenterLatitude,
                row.CenterLongitude,
                row.BoundaryGeoJson);
    }
}
