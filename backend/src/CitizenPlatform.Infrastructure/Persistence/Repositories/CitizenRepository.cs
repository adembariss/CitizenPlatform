using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class CitizenRepository : ICitizenRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public CitizenRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Citizen citizen, CancellationToken cancellationToken)
    {
        await _dbContext.Citizens.AddAsync(citizen, cancellationToken);
    }
}
