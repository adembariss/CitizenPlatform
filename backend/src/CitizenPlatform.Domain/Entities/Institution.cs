using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

/// <summary>
/// Belediye dışı hizmet kurumu (elektrik/su/doğalgaz dağıtım şirketi). Bölgesel hizmet verir;
/// vatandaş konumuna göre <see cref="ServiceAreas"/> üzerinden eşleşir. Şikayet/panel/kullanıcı
/// altyapısı belediye ile aynı şekilde çalışır (kurum bir tenant'tır).
/// </summary>
public sealed class Institution : AuditableEntity
{
    private readonly List<InstitutionServiceArea> _serviceAreas = [];

    private Institution()
    {
    }

    private Institution(Guid id, string name, string code, InstitutionType type)
        : base(id)
    {
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Code = Guard.AgainstEmpty(code, nameof(code), 50).ToUpperInvariant();
        Type = type;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public InstitutionType Type { get; private set; }

    public bool IsActive { get; private set; }

    // Merkez ili ve yaklaşık koordinatı (adresten seçimde şikayet konumu olarak kullanılabilir).
    public string? Province { get; private set; }

    public double? CenterLatitude { get; private set; }

    public double? CenterLongitude { get; private set; }

    public IReadOnlyCollection<InstitutionServiceArea> ServiceAreas => _serviceAreas.AsReadOnly();

    public static Institution Create(string name, string code, InstitutionType type, string? province = null)
    {
        return new Institution(Guid.NewGuid(), name, code, type)
        {
            Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim()
        };
    }

    public void SetCenter(double latitude, double longitude)
    {
        CenterLatitude = latitude;
        CenterLongitude = longitude;
        Touch();
    }

    public void AddServiceArea(string province, string? district = null)
    {
        _serviceAreas.Add(InstitutionServiceArea.Create(Id, province, district));
        Touch();
    }

    public void Rename(string name)
    {
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
