using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<IntegrationOutboxMessage>> GetDueAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken)
    {
        return await _dbContext.IntegrationOutbox
            .Where(message => message.Status == OutboxStatus.Pending
                && (message.NextRetryAt == null || message.NextRetryAt <= utcNow))
            .OrderBy(message => message.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
