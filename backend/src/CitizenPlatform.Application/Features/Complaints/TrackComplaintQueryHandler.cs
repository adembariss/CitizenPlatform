using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed class TrackComplaintQueryHandler
{
    private readonly IPublicComplaintTrackingRepository _repository;

    public TrackComplaintQueryHandler(IPublicComplaintTrackingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PublicComplaintTrackingDto?> HandleAsync(string trackingCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var row = await _repository.GetByTrackingCodeAsync(trackingCode.Trim(), cancellationToken);

        return row is null ? null : Map(row);
    }

    internal static PublicComplaintTrackingDto Map(PublicComplaintTrackingRow row)
    {
        var complaint = row.Complaint;

        // Citizen-facing view: no personal data echo, no internal comments,
        // no user ids, and only history entries flagged as visible to the citizen.
        var statusHistory = complaint.StatusHistories
            .Where(history => history.IsVisibleToCitizen)
            .OrderBy(history => history.CreatedAt)
            .Select(history => new PublicComplaintStatusHistoryDto(
                history.PreviousStatus,
                history.NewStatus,
                history.Note,
                history.CreatedAt))
            .ToArray();

        var responses = complaint.Comments
            .Where(comment => !comment.IsInternal)
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new PublicComplaintResponseDto(comment.Body, comment.CreatedAt))
            .ToArray();

        return new PublicComplaintTrackingDto(
            complaint.TrackingCode,
            row.MunicipalityName,
            row.CategoryName,
            row.CurrentDepartmentName,
            complaint.Title,
            complaint.Description,
            complaint.AddressText,
            complaint.Status,
            complaint.CreatedAt,
            complaint.UpdatedAt,
            complaint.ClosedAt,
            complaint.Attachments.Count,
            statusHistory,
            responses,
            complaint.InstitutionId is not null);
    }
}
