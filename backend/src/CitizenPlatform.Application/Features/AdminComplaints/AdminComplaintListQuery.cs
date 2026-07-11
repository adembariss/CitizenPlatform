using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed record AdminComplaintListQuery(
    ComplaintStatus? Status,
    Guid? CategoryId,
    Guid? DepartmentId,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? Search,
    int Page,
    int PageSize,
    Guid? MunicipalityId);
