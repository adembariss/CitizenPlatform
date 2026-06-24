using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class IntegrationOutboxRepository : IIntegrationOutboxRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public IntegrationOutboxRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(IntegrationOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        await _dbContext.IntegrationOutbox.AddAsync(outboxMessage, cancellationToken);
    }
}
