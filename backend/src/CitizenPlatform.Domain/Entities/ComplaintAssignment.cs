using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class ComplaintAssignment : AuditableEntity
{
    private ComplaintAssignment()
    {
    }

    private ComplaintAssignment(
        Guid id,
        Guid complaintId,
        Guid departmentId,
        Guid assignedByUserId,
        Guid? assignedUserId,
        string? note)
        : base(id)
    {
        ComplaintId = Guard.AgainstEmpty(complaintId, nameof(complaintId));
        DepartmentId = Guard.AgainstEmpty(departmentId, nameof(departmentId));
        AssignedByUserId = Guard.AgainstEmpty(assignedByUserId, nameof(assignedByUserId));
        AssignedUserId = assignedUserId == Guid.Empty ? null : assignedUserId;
        AssignedAt = DateTimeOffset.UtcNow;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid ComplaintId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public string? Note { get; private set; }

    public static ComplaintAssignment Create(
        Guid complaintId,
        Guid departmentId,
        Guid assignedByUserId,
        Guid? assignedUserId = null,
        string? note = null)
    {
        return new ComplaintAssignment(Guid.NewGuid(), complaintId, departmentId, assignedByUserId, assignedUserId, note);
    }
}
