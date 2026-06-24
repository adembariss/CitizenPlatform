using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IIntegrationOutboxRepository
{
    Task AddAsync(IntegrationOutboxMessage outboxMessage, CancellationToken cancellationToken);
}
