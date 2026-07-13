using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class MunicipalityRepository : IMunicipalityRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public MunicipalityRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Municipalities.FirstOrDefaultAsync(municipality => municipality.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetProvincesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Municipalities
            .AsNoTracking()
            .Where(municipality => municipality.IsActive && municipality.Province != null)
            .Select(municipality => municipality.Province!)
            .Distinct()
            .OrderBy(province => province)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DistrictRow>> GetDistrictsByProvinceAsync(string province, CancellationToken cancellationToken)
    {
        return await _dbContext.Municipalities
            .AsNoTracking()
            .Where(municipality => municipality.IsActive && municipality.Province == province)
            .OrderBy(municipality => municipality.Name)
            .Select(municipality => new DistrictRow(
                municipality.Id,
                municipality.Name,
                municipality.Code,
                municipality.CenterLatitude,
                municipality.CenterLongitude))
            .ToListAsync(cancellationToken);
    }
}
