using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public sealed record AdminComplaintSearchCriteria(
    Guid? MunicipalityId,
    ComplaintStatus? Status,
    Guid? CategoryId,
    Guid? DepartmentId,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? Search,
    int Page,
    int PageSize,
    string? Province = null);

public sealed record AdminComplaintListRow(
    Guid Id,
    string TrackingCode,
    string MunicipalityName,
    string CategoryName,
    string? DepartmentName,
    string Title,
    string Description,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    string? CitizenFullName,
    string? AddressText,
    double Latitude,
    double Longitude,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AdminComplaintPagedRows(
    IReadOnlyList<AdminComplaintListRow> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AdminComplaintDetailRow(
    Complaint Complaint,
    string MunicipalityName,
    string CategoryName,
    string? DepartmentName,
    string? CitizenFullName,
    string? CitizenPhoneNumber,
    string? CitizenEmail,
    IReadOnlyDictionary<Guid, string> DepartmentNamesById);

public interface IAdminComplaintQueryRepository
{
    Task<AdminComplaintPagedRows> SearchAsync(AdminComplaintSearchCriteria criteria, CancellationToken cancellationToken);

    Task<AdminComplaintDetailRow?> GetDetailAsync(Guid complaintId, Guid? tenantMunicipalityId, CancellationToken cancellationToken);
}
