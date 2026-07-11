using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IMunicipalityRepository
{
    Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
