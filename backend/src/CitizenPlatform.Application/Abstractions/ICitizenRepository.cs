using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface ICitizenRepository
{
    Task AddAsync(Citizen citizen, CancellationToken cancellationToken);

    Task<Citizen?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Citizen?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
