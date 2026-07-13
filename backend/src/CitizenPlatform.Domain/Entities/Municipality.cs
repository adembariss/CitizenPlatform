using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class Municipality : AuditableEntity
{
    private readonly List<MunicipalityBoundary> _boundaries = [];
    private readonly List<Department> _departments = [];

    private Municipality()
    {
    }

    private Municipality(Guid id, string name, string code)
        : base(id)
    {
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Code = Guard.AgainstEmpty(code, nameof(code), 50).ToUpperInvariant();
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    // İl adı (adres seçici il→ilçe gruplaması için) ve belediye merkezinin yaklaşık
    // koordinatı (adresten seçimde şikayet konumu olarak kullanılır). Seed ile doldurulur.
    public string? Province { get; private set; }

    public double? CenterLatitude { get; private set; }

    public double? CenterLongitude { get; private set; }

    public IReadOnlyCollection<MunicipalityBoundary> Boundaries => _boundaries.AsReadOnly();

    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    public static Municipality Create(string name, string code)
    {
        return new Municipality(Guid.NewGuid(), name, code);
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
