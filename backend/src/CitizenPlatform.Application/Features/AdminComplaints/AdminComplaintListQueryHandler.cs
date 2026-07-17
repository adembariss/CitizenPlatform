using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed class AdminComplaintListQueryHandler
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IAdminComplaintQueryRepository _repository;

    public AdminComplaintListQueryHandler(IAdminComplaintQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdminComplaintListResponseDto> HandleAsync(
        AdminComplaintListQuery query,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);
        var municipalityId = scope.ResolveListFilter(query.MunicipalityId);

        var criteria = new AdminComplaintSearchCriteria(
            municipalityId,
            query.Status,
            query.CategoryId,
            query.DepartmentId,
            query.DateFrom,
            query.DateTo,
            query.Search,
            page,
            pageSize,
            query.Province,
            scope.InstitutionId);

        var rows = await _repository.SearchAsync(criteria, cancellationToken);

        var items = rows.Items
            .Select(row => new AdminComplaintListItemDto(
                row.Id,
                row.TrackingCode,
                row.MunicipalityName,
                row.CategoryName,
                row.DepartmentName,
                row.Title,
                Summarize(row.Description),
                row.Status,
                row.Priority,
                PersonalDataMasking.MaskFullName(row.CitizenFullName),
                row.AddressText,
                row.Latitude,
                row.Longitude,
                row.CreatedAt,
                row.UpdatedAt))
            .ToArray();

        return new AdminComplaintListResponseDto(items, rows.Page, rows.PageSize, rows.TotalCount);
    }

    private static string Summarize(string description)
    {
        const int maxLength = 160;
        var trimmed = description.Trim();
        return trimmed.Length <= maxLength ? trimmed : string.Concat(trimmed.AsSpan(0, maxLength), "...");
    }
}
