using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface ICategoryDepartmentRuleRepository
{
    Task<CategoryDepartmentRule?> GetActiveRuleAsync(
        Guid municipalityId,
        Guid categoryId,
        CancellationToken cancellationToken);
}
