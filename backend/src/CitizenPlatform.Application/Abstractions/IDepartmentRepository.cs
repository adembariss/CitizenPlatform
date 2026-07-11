using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
