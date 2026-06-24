using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class ComplaintComment : AuditableEntity
{
    private ComplaintComment()
    {
    }

    private ComplaintComment(Guid id, Guid complaintId, Guid authorUserId, string body, bool isInternal)
        : base(id)
    {
        ComplaintId = Guard.AgainstEmpty(complaintId, nameof(complaintId));
        AuthorUserId = Guard.AgainstEmpty(authorUserId, nameof(authorUserId));
        Body = Guard.AgainstEmpty(body, nameof(body), 4000);
        IsInternal = isInternal;
    }

    public Guid ComplaintId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public bool IsInternal { get; private set; }

    public static ComplaintComment Create(Guid complaintId, Guid authorUserId, string body, bool isInternal = false)
    {
        return new ComplaintComment(Guid.NewGuid(), complaintId, authorUserId, body, isInternal);
    }
}
