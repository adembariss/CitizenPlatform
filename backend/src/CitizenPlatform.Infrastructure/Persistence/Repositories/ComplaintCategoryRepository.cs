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
                    && category.InstitutionId == null
                    && (category.MunicipalityId == null || category.MunicipalityId == municipalityId),
                cancellationToken);
    }

    public async Task<ComplaintCategory?> GetActiveForInstitutionAsync(
        Guid categoryId,
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ComplaintCategories
            .FirstOrDefaultAsync(
                category => category.Id == categoryId
                    && category.IsActive
                    && category.InstitutionId == institutionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ComplaintCategory>> ListForInstitutionAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        return await _dbContext.ComplaintCategories
            .Where(category => category.InstitutionId == institutionId && category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ComplaintCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.ComplaintCategories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ComplaintCategory>> ListAsync(Guid? municipalityId, CancellationToken cancellationToken)
    {
        // Kurum kategorileri (institution_id dolu) belediye listelerine karışmaz.
        var query = _dbContext.ComplaintCategories.Where(category => category.InstitutionId == null);

        if (municipalityId is not null)
        {
            query = query.Where(category => category.MunicipalityId == null || category.MunicipalityId == municipalityId);
        }

        return await query.OrderBy(category => category.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(Guid? municipalityId, string code, CancellationToken cancellationToken)
    {
        return await _dbContext.ComplaintCategories.AnyAsync(
            category => category.Code == code && category.MunicipalityId == municipalityId,
            cancellationToken);
    }

    public async Task AddAsync(ComplaintCategory category, CancellationToken cancellationToken)
    {
        await _dbContext.ComplaintCategories.AddAsync(category, cancellationToken);
    }
}
