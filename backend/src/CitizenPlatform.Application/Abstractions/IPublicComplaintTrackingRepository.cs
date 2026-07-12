using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public sealed record PublicComplaintTrackingRow(
    Complaint Complaint,
    string MunicipalityName,
    string CategoryName,
    string? CurrentDepartmentName);

public interface IPublicComplaintTrackingRepository
{
    Task<PublicComplaintTrackingRow?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken);
}
