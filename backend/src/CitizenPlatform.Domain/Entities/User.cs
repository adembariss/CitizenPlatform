using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

public sealed class User : AuditableEntity
{
    private User()
    {
    }

    private User(Guid id, string email, string displayName, UserType userType)
        : base(id)
    {
        Email = Guard.AgainstEmpty(email, nameof(email), 320).ToLowerInvariant();
        DisplayName = Guard.AgainstEmpty(displayName, nameof(displayName), 200);
        UserType = userType;
        IsActive = true;
    }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public UserType UserType { get; private set; }

    public bool IsActive { get; private set; }

    public static User Create(string email, string displayName, UserType userType)
    {
        return new User(Guid.NewGuid(), email, displayName, userType);
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
