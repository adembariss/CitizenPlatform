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
}
