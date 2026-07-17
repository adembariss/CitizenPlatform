using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IComplaintCategoryRepository
{
    Task<ComplaintCategory?> GetActiveForMunicipalityAsync(
        Guid categoryId,
        Guid municipalityId,
        CancellationToken cancellationToken);

    Task<ComplaintCategory?> GetActiveForInstitutionAsync(
        Guid categoryId,
        Guid institutionId,
        CancellationToken cancellationToken);

    Task<ComplaintCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ComplaintCategory>> ListAsync(Guid? municipalityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ComplaintCategory>> ListForInstitutionAsync(Guid institutionId, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(Guid? municipalityId, string code, CancellationToken cancellationToken);

    Task AddAsync(ComplaintCategory category, CancellationToken cancellationToken);
}
