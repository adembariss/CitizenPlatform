using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.AdminCategories;

public sealed class AdminCategoryListQueryHandler
{
    private readonly IComplaintCategoryRepository _categoryRepository;

    public AdminCategoryListQueryHandler(IComplaintCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(
        Guid? requestedMunicipalityId,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var municipalityId = scope.ResolveListFilter(requestedMunicipalityId);
        var categories = await _categoryRepository.ListAsync(municipalityId, cancellationToken);

        return categories
            .Select(category => new CategoryDto(category.Id, category.MunicipalityId, category.Name, category.Code, category.IsActive))
            .ToArray();
    }
}
