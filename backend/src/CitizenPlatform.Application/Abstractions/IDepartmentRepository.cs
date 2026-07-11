using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> ListAsync(Guid? municipalityId, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(Guid municipalityId, string code, CancellationToken cancellationToken);

    Task AddAsync(Department department, CancellationToken cancellationToken);
}
