using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

public sealed class ComplaintStatusHistory : AuditableEntity
{
    private ComplaintStatusHistory()
    {
    }

    private ComplaintStatusHistory(
        Guid id,
        Guid complaintId,
        ComplaintStatus previousStatus,
        ComplaintStatus newStatus,
        Guid changedByUserId,
        string? note)
        : base(id)
    {
        ComplaintId = Guard.AgainstEmpty(complaintId, nameof(complaintId));
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = Guard.AgainstEmpty(changedByUserId, nameof(changedByUserId));
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid ComplaintId { get; private set; }

    public ComplaintStatus PreviousStatus { get; private set; }

    public ComplaintStatus NewStatus { get; private set; }

    public Guid ChangedByUserId { get; private set; }

    public string? Note { get; private set; }

    public static ComplaintStatusHistory Create(
        Guid complaintId,
        ComplaintStatus previousStatus,
        ComplaintStatus newStatus,
        Guid changedByUserId,
        string? note = null)
    {
        if (previousStatus == newStatus)
        {
            throw new InvalidOperationException("Previous and new status cannot be the same.");
        }

        return new ComplaintStatusHistory(Guid.NewGuid(), complaintId, previousStatus, newStatus, changedByUserId, note);
    }
}
