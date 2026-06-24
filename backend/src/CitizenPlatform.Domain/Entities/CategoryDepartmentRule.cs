using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

public sealed class CategoryDepartmentRule : AuditableEntity
{
    private CategoryDepartmentRule()
    {
    }

    private CategoryDepartmentRule(
        Guid id,
        Guid municipalityId,
        Guid categoryId,
        Guid departmentId,
        ComplaintPriority defaultPriority)
        : base(id)
    {
        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        CategoryId = Guard.AgainstEmpty(categoryId, nameof(categoryId));
        DepartmentId = Guard.AgainstEmpty(departmentId, nameof(departmentId));
        DefaultPriority = defaultPriority;
        IsActive = true;
    }

    public Guid MunicipalityId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public ComplaintPriority DefaultPriority { get; private set; }

    public bool IsActive { get; private set; }

    public static CategoryDepartmentRule Create(
        Guid municipalityId,
        Guid categoryId,
        Guid departmentId,
        ComplaintPriority defaultPriority = ComplaintPriority.Normal)
    {
        return new CategoryDepartmentRule(Guid.NewGuid(), municipalityId, categoryId, departmentId, defaultPriority);
    }
}
