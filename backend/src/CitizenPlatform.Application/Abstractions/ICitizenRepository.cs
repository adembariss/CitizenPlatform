using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface ICitizenRepository
{
    Task AddAsync(Citizen citizen, CancellationToken cancellationToken);
}
