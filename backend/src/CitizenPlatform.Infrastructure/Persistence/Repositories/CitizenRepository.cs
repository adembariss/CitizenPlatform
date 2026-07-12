using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Citizen?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Citizens.FirstOrDefaultAsync(citizen => citizen.Id == id, cancellationToken);
    }

    public async Task<Citizen?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Citizens.FirstOrDefaultAsync(citizen => citizen.UserId == userId, cancellationToken);
    }
}
