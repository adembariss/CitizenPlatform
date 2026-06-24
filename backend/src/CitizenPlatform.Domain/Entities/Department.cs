using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class Department : AuditableEntity
{
    private Department()
    {
    }

    private Department(Guid id, Guid municipalityId, string name, string code)
        : base(id)
    {
        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        Code = Guard.AgainstEmpty(code, nameof(code), 50).ToUpperInvariant();
        IsActive = true;
    }

    public Guid MunicipalityId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Department Create(Guid municipalityId, string name, string code)
    {
        return new Department(Guid.NewGuid(), municipalityId, name, code);
    }
}
