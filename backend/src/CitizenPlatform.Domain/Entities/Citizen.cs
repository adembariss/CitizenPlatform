using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class Citizen : AuditableEntity
{
    private Citizen()
    {
    }

    private Citizen(Guid id, Guid? userId, string fullName, string? phoneNumber, string? email)
        : base(id)
    {
        UserId = userId == Guid.Empty ? null : userId;
        FullName = Guard.AgainstEmpty(fullName, nameof(fullName), 200);
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public Guid? UserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? Email { get; private set; }

    public static Citizen Create(Guid? userId, string fullName, string? phoneNumber = null, string? email = null)
    {
        return new Citizen(Guid.NewGuid(), userId, fullName, phoneNumber, email);
    }
}
