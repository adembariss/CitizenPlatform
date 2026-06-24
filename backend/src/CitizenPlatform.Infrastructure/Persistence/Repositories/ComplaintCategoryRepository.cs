using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class ComplaintCategoryRepository : IComplaintCategoryRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public ComplaintCategoryRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ComplaintCategory?> GetActiveForMunicipalityAsync(
        Guid categoryId,
        Guid municipalityId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ComplaintCategories
            .FirstOrDefaultAsync(
                category => category.Id == categoryId
                    && category.IsActive
                    && (category.MunicipalityId == null || category.MunicipalityId == municipalityId),
                cancellationToken);
    }
}
