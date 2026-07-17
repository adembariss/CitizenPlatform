using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class Department : AuditableEntity
{
    private Department()
    {
    }

    private Department(Guid id, Guid? municipalityId, Guid? institutionId, string name, string code)
        : base(id)
    {
        MunicipalityId = municipalityId == Guid.Empty ? null : municipalityId;
        InstitutionId = institutionId == Guid.Empty ? null : institutionId;
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Code = Guard.AgainstEmpty(code, nameof(code), 50).ToUpperInvariant();
        IsActive = true;
    }

    /// <summary>Belediye birimi ise dolu; kurum biriminde boştur.</summary>
    public Guid? MunicipalityId { get; private set; }

    /// <summary>Dağıtım kurumu (elektrik/su/doğalgaz) birimi ise dolu; belediye biriminde boştur.</summary>
    public Guid? InstitutionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Department Create(Guid municipalityId, string name, string code)
    {
        return new Department(Guid.NewGuid(), Guard.AgainstEmpty(municipalityId, nameof(municipalityId)), null, name, code);
    }

    /// <summary>Dağıtım kurumuna ait birim oluşturur (belediye birimlerinden ayrı yapı).</summary>
    public static Department CreateForInstitution(Guid institutionId, string name, string code)
    {
        return new Department(Guid.NewGuid(), null, Guard.AgainstEmpty(institutionId, nameof(institutionId)), name, code);
    }

    public void Rename(string name)
    {
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
