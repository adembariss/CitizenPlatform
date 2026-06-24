using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IComplaintCategoryRepository
{
    Task<ComplaintCategory?> GetActiveForMunicipalityAsync(
        Guid categoryId,
        Guid municipalityId,
        CancellationToken cancellationToken);
}
