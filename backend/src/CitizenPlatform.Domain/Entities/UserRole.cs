using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class UserRole : AuditableEntity
{
    private UserRole()
    {
    }

    private UserRole(Guid id, Guid userId, Guid roleId, Guid? municipalityId)
        : base(id)
    {
        UserId = Guard.AgainstEmpty(userId, nameof(userId));
        RoleId = Guard.AgainstEmpty(roleId, nameof(roleId));
        MunicipalityId = municipalityId == Guid.Empty ? null : municipalityId;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid? MunicipalityId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static UserRole Assign(Guid userId, Guid roleId, Guid? municipalityId = null)
    {
        return new UserRole(Guid.NewGuid(), userId, roleId, municipalityId);
    }

    public void Revoke()
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
