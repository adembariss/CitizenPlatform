using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class Pharmacy : AuditableEntity
{
    private Pharmacy()
    {
    }

    private Pharmacy(
        Guid id,
        string name,
        string province,
        string district,
        string? addressText,
        string? phoneNumber,
        double latitude,
        double longitude,
        bool isOnDuty)
        : base(id)
    {
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Province = Guard.AgainstEmpty(province, nameof(province), 100);
        District = Guard.AgainstEmpty(district, nameof(district), 100);
        AddressText = string.IsNullOrWhiteSpace(addressText) ? null : addressText.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Latitude = latitude;
        Longitude = longitude;
        IsOnDuty = isOnDuty;
    }

    public string Name { get; private set; } = string.Empty;

    public string Province { get; private set; } = string.Empty;

    public string District { get; private set; } = string.Empty;

    public string? AddressText { get; private set; }

    public string? PhoneNumber { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public bool IsOnDuty { get; private set; }

    public static Pharmacy Create(
        string name,
        string province,
        string district,
        string? addressText,
        string? phoneNumber,
        double latitude,
        double longitude,
        bool isOnDuty)
    {
        return new Pharmacy(Guid.NewGuid(), name, province, district, addressText, phoneNumber, latitude, longitude, isOnDuty);
    }

    public void SetOnDuty(bool isOnDuty)
    {
        IsOnDuty = isOnDuty;
        Touch();
    }
}
