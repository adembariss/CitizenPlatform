using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    private AuditLog()
    {
    }

    private AuditLog(
        Guid id,
        string entityName,
        Guid entityId,
        string action,
        string? changes,
        Guid? userId,
        string? ipAddress)
        : base(id)
    {
        EntityName = Guard.AgainstEmpty(entityName, nameof(entityName), 200);
        EntityId = Guard.AgainstEmpty(entityId, nameof(entityId));
        Action = Guard.AgainstEmpty(action, nameof(action), 100);
        Changes = string.IsNullOrWhiteSpace(changes) ? null : changes.Trim();
        UserId = userId == Guid.Empty ? null : userId;
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
    }

    public string EntityName { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string? Changes { get; private set; }

    public Guid? UserId { get; private set; }

    public string? IpAddress { get; private set; }

    public static AuditLog Create(
        string entityName,
        Guid entityId,
        string action,
        string? changes = null,
        Guid? userId = null,
        string? ipAddress = null)
    {
        return new AuditLog(Guid.NewGuid(), entityName, entityId, action, changes, userId, ipAddress);
    }
}
