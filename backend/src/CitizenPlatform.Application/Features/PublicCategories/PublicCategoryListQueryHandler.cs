using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.PublicCategories;

public sealed class PublicCategoryListQueryHandler
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IComplaintCategoryRepository _categoryRepository;

    public PublicCategoryListQueryHandler(
        IMunicipalityRepository municipalityRepository,
        IComplaintCategoryRepository categoryRepository)
    {
        _municipalityRepository = municipalityRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<PublicCategoryDto>?> HandleAsync(
        Guid municipalityId,
        CancellationToken cancellationToken)
    {
        var municipality = await _municipalityRepository.GetByIdAsync(municipalityId, cancellationToken);
        if (municipality is null || !municipality.IsActive)
        {
            return null;
        }

        var categories = await _categoryRepository.ListAsync(municipalityId, cancellationToken);

        return categories
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new PublicCategoryDto(category.Id, category.Name, category.Code))
            .ToArray();
    }
}
