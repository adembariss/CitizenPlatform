using CitizenPlatform.Domain.Common;

namespace CitizenPlatform.Domain.Entities;

/// <summary>
/// Bir kurumun hizmet verdiği bölge. <see cref="District"/> boşsa ilin tamamı kapsanır.
/// </summary>
public sealed class InstitutionServiceArea : AuditableEntity
{
    private InstitutionServiceArea()
    {
    }

    private InstitutionServiceArea(Guid id, Guid institutionId, string province, string? district)
        : base(id)
    {
        InstitutionId = Guard.AgainstEmpty(institutionId, nameof(institutionId));
        Province = Guard.AgainstEmpty(province, nameof(province), 100);
        District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
    }

    public Guid InstitutionId { get; private set; }

    public string Province { get; private set; } = string.Empty;

    /// <summary>Boş (null) ise ilin tamamı.</summary>
    public string? District { get; private set; }

    public static InstitutionServiceArea Create(Guid institutionId, string province, string? district = null)
    {
        return new InstitutionServiceArea(Guid.NewGuid(), institutionId, province, district);
    }
}
