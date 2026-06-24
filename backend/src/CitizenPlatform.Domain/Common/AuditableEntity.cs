namespace CitizenPlatform.Domain.Common;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public DateTimeOffset? DeletedAt { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public virtual void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    protected void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
