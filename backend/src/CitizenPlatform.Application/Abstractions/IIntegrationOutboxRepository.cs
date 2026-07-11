using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IIntegrationOutboxRepository
{
    Task AddAsync(IntegrationOutboxMessage outboxMessage, CancellationToken cancellationToken);

    Task<IReadOnlyList<IntegrationOutboxMessage>> GetDueAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);
}
