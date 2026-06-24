using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class CategoryDepartmentRuleRepository : ICategoryDepartmentRuleRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public CategoryDepartmentRuleRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryDepartmentRule?> GetActiveRuleAsync(
        Guid municipalityId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CategoryDepartmentRules
            .FirstOrDefaultAsync(
                rule => rule.MunicipalityId == municipalityId
                    && rule.CategoryId == categoryId
                    && rule.IsActive,
                cancellationToken);
    }
}
