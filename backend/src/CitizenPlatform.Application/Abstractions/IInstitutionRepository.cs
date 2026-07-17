using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IInstitutionRepository
{
    Task<Institution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Verilen il (ve varsa ilçe) için hizmet veren aktif kurumları döndürür.</summary>
    Task<IReadOnlyList<Institution>> ListByAreaAsync(string province, string? district, CancellationToken cancellationToken);
}
