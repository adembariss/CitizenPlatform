using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Tenant'a göre birimler. <paramref name="institutionId"/> doluysa yalnızca o kurumun birimleri;
    /// aksi halde kurum birimleri hariç tutulur ve (verilmişse) belediyeye göre filtrelenir.
    /// </summary>
    Task<IReadOnlyList<Department>> ListAsync(Guid? municipalityId, Guid? institutionId, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(Guid municipalityId, string code, CancellationToken cancellationToken);

    Task AddAsync(Department department, CancellationToken cancellationToken);
}
