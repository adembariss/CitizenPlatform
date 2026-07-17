using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class ComplaintCategory : AuditableEntity
{
    private ComplaintCategory()
    {
    }

    private ComplaintCategory(Guid id, Guid? municipalityId, Guid? institutionId, string name, string code)
        : base(id)
    {
        MunicipalityId = municipalityId == Guid.Empty ? null : municipalityId;
        InstitutionId = institutionId == Guid.Empty ? null : institutionId;
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Code = Guard.AgainstEmpty(code, nameof(code), 50).ToUpperInvariant();
        IsActive = true;
    }

    public Guid? MunicipalityId { get; private set; }

    // Belediye dışı kuruma ait kategori (elektrik/su/doğalgaz). Belediye kategorilerinde boştur.
    public Guid? InstitutionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static ComplaintCategory Create(string name, string code, Guid? municipalityId = null, Guid? institutionId = null)
    {
        return new ComplaintCategory(Guid.NewGuid(), municipalityId, institutionId, name, code);
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
