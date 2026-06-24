using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

public sealed class MunicipalityBoundary : AuditableEntity
{
    private MunicipalityBoundary()
    {
    }

    private MunicipalityBoundary(Guid id, Guid municipalityId, string name, string boundaryGeometry)
        : base(id)
    {
        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        Name = Guard.AgainstEmpty(name, nameof(name), 200);
        BoundaryGeometry = Guard.AgainstEmpty(boundaryGeometry, nameof(boundaryGeometry), 100_000);
        IsActive = true;
    }

    public Guid MunicipalityId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string BoundaryGeometry { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static MunicipalityBoundary Create(Guid municipalityId, string name, string boundaryGeometry)
    {
        return new MunicipalityBoundary(Guid.NewGuid(), municipalityId, name, boundaryGeometry);
    }

    public void ReplaceGeometry(string boundaryGeometry)
    {
        BoundaryGeometry = Guard.AgainstEmpty(boundaryGeometry, nameof(boundaryGeometry), 100_000);
        Touch();
    }
}
