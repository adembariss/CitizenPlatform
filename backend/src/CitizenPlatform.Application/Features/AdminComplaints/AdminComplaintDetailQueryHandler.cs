using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed class AdminComplaintDetailQueryHandler
{
    private readonly IAdminComplaintQueryRepository _repository;

    public AdminComplaintDetailQueryHandler(IAdminComplaintQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdminComplaintDetailDto?> HandleAsync(
        Guid complaintId,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var tenantMunicipalityId = scope.IsSystemAdmin ? (Guid?)null : scope.MunicipalityId;
        var row = await _repository.GetDetailAsync(complaintId, tenantMunicipalityId, cancellationToken);

        return row is null ? null : Map(row);
    }

    internal static AdminComplaintDetailDto Map(AdminComplaintDetailRow row)
    {
        var complaint = row.Complaint;

        return new AdminComplaintDetailDto(
            complaint.Id,
            complaint.TrackingCode,
            complaint.MunicipalityId,
            row.MunicipalityName,
            complaint.CategoryId,
            row.CategoryName,
            complaint.CurrentDepartmentId,
            row.DepartmentName,
            complaint.Title,
            complaint.Description,
            row.CitizenFullName,
            row.CitizenPhoneNumber,
            row.CitizenEmail,
            complaint.AddressText,
            complaint.Location.Latitude,
            complaint.Location.Longitude,
            complaint.Status,
            complaint.Priority,
            complaint.Source,
            complaint.CreatedAt,
            complaint.UpdatedAt,
            complaint.ClosedAt,
            complaint.Attachments.Select(MapAttachment).ToArray(),
            complaint.StatusHistories.OrderBy(history => history.CreatedAt).Select(MapStatusHistory).ToArray(),
            complaint.Comments.OrderBy(comment => comment.CreatedAt).Select(MapComment).ToArray(),
            complaint.Assignments.OrderBy(assignment => assignment.AssignedAt).Select(MapAssignment(row.DepartmentNamesById)).ToArray());
    }

    private static AdminComplaintAttachmentDto MapAttachment(ComplaintAttachment attachment)
    {
        return new AdminComplaintAttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeInBytes,
            attachment.Sha256Hash,
            attachment.CreatedAt);
    }

    internal static AdminComplaintStatusHistoryDto MapStatusHistory(ComplaintStatusHistory history)
    {
        return new AdminComplaintStatusHistoryDto(
            history.Id,
            history.PreviousStatus,
            history.NewStatus,
            history.ChangedByUserId,
            history.Note,
            history.IsVisibleToCitizen,
            history.CreatedAt);
    }

    internal static AdminComplaintCommentDto MapComment(ComplaintComment comment)
    {
        return new AdminComplaintCommentDto(
            comment.Id,
            comment.AuthorUserId,
            comment.Body,
            comment.IsInternal,
            comment.CreatedAt);
    }

    private static Func<ComplaintAssignment, AdminComplaintAssignmentDto> MapAssignment(
        IReadOnlyDictionary<Guid, string> departmentNamesById)
    {
        return assignment => new AdminComplaintAssignmentDto(
            assignment.Id,
            assignment.DepartmentId,
            departmentNamesById.GetValueOrDefault(assignment.DepartmentId, string.Empty),
            assignment.AssignedByUserId,
            assignment.AssignedUserId,
            assignment.Note,
            assignment.AssignedAt);
    }
}
