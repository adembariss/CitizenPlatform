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

    public bool PhoneVerified { get; private set; }

    public string? PhoneVerificationCode { get; private set; }

    public DateTimeOffset? PhoneVerificationExpiresAt { get; private set; }

    public static Citizen Create(Guid? userId, string fullName, string? phoneNumber = null, string? email = null)
    {
        return new Citizen(Guid.NewGuid(), userId, fullName, phoneNumber, email);
    }

    public void StartPhoneVerification(string code, DateTimeOffset expiresAt)
    {
        PhoneVerificationCode = Guard.AgainstEmpty(code, nameof(code), 12);
        PhoneVerificationExpiresAt = expiresAt;
        PhoneVerified = false;
        Touch();
    }

    /// <summary>Verifies the SMS code. Returns false for a wrong or expired code.</summary>
    public bool VerifyPhone(string code, DateTimeOffset now)
    {
        if (PhoneVerified)
        {
            return true;
        }

        if (string.IsNullOrEmpty(PhoneVerificationCode)
            || PhoneVerificationExpiresAt is null
            || now > PhoneVerificationExpiresAt
            || !string.Equals(PhoneVerificationCode, code?.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        PhoneVerified = true;
        PhoneVerificationCode = null;
        PhoneVerificationExpiresAt = null;
        Touch();
        return true;
    }
}
