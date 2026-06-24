using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class Role : AuditableEntity
{
    private Role()
    {
    }

    private Role(Guid id, string name, string key, bool isSystemRole)
        : base(id)
    {
        Name = Guard.AgainstEmpty(name, nameof(name), 100);
        Key = Guard.AgainstEmpty(key, nameof(key), 100).ToUpperInvariant();
        IsSystemRole = isSystemRole;
    }

    public string Name { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    public bool IsSystemRole { get; private set; }

    public static Role Create(string name, string key, bool isSystemRole = false)
    {
        return new Role(Guid.NewGuid(), name, key, isSystemRole);
    }
}
