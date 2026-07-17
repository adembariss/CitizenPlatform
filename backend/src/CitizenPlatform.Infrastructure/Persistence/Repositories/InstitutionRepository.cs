using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class InstitutionRepository : IInstitutionRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public InstitutionRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Institution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Institutions.FirstOrDefaultAsync(institution => institution.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Institution>> ListByAreaAsync(string province, string? district, CancellationToken cancellationToken)
    {
        var normalizedProvince = province.Trim();
        var normalizedDistrict = string.IsNullOrWhiteSpace(district) ? null : district.Trim();

        // Kurum, ili kapsıyorsa eşleşir: hizmet alanı ilin tamamı (district NULL) ya da
        // istenen ilçeyle birebir. İlçe verilmezse ildeki tüm kurumlar döner.
        var query =
            from institution in _dbContext.Institutions
            where institution.IsActive
            join area in _dbContext.InstitutionServiceAreas
                on institution.Id equals area.InstitutionId
            where area.Province == normalizedProvince
                  && (area.District == null
                      || normalizedDistrict == null
                      || area.District == normalizedDistrict)
            select institution;

        return await query
            .Distinct()
            .OrderBy(institution => institution.Type)
            .ThenBy(institution => institution.Name)
            .ToListAsync(cancellationToken);
    }
}
