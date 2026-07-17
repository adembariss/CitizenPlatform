using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed class AdminComplaintHistoryQueryHandler
{
    private readonly IAdminComplaintQueryRepository _repository;

    public AdminComplaintHistoryQueryHandler(IAdminComplaintQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdminComplaintHistoryDto?> HandleAsync(
        Guid complaintId,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var row = await _repository.GetDetailAsync(complaintId, null, cancellationToken);
        if (row is null || !scope.CanAccessComplaint(row.Complaint.MunicipalityId, row.Complaint.InstitutionId))
        {
            return null;
        }

        var detail = AdminComplaintDetailQueryHandler.Map(row);

        return new AdminComplaintHistoryDto(
            detail.Id,
            detail.CreatedAt,
            detail.UpdatedAt,
            detail.StatusHistories,
            detail.Comments,
            detail.Assignments);
    }
}
