using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Features.PublicInstitutions;

internal static class InstitutionTypeLabels
{
    public static string Label(InstitutionType type) => type switch
    {
        InstitutionType.Electricity => "Elektrik",
        InstitutionType.Water => "Su",
        InstitutionType.NaturalGas => "Doğalgaz",
        _ => type.ToString()
    };
}

public sealed class PublicInstitutionListQueryHandler
{
    private readonly IInstitutionRepository _institutionRepository;
    private readonly IMunicipalityRepository _municipalityRepository;

    public PublicInstitutionListQueryHandler(
        IInstitutionRepository institutionRepository,
        IMunicipalityRepository municipalityRepository)
    {
        _institutionRepository = institutionRepository;
        _municipalityRepository = municipalityRepository;
    }

    public async Task<IReadOnlyList<PublicInstitutionDto>> HandleAsync(
        string? province,
        string? district,
        Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        // Harita modunda vatandaş yalnızca belediyeyi çözer; ilini municipalityId'den türet.
        if (string.IsNullOrWhiteSpace(province) && municipalityId is Guid id)
        {
            var municipality = await _municipalityRepository.GetByIdAsync(id, cancellationToken);
            province = municipality?.Province;
            district = null;
        }

        if (string.IsNullOrWhiteSpace(province))
        {
            return Array.Empty<PublicInstitutionDto>();
        }

        var institutions = await _institutionRepository.ListByAreaAsync(province, district, cancellationToken);

        return institutions
            .Select(institution => new PublicInstitutionDto(
                institution.Id,
                institution.Name,
                institution.Type.ToString(),
                InstitutionTypeLabels.Label(institution.Type)))
            .ToArray();
    }
}

public sealed class PublicInstitutionCategoryListQueryHandler
{
    private readonly IInstitutionRepository _institutionRepository;
    private readonly IComplaintCategoryRepository _categoryRepository;

    public PublicInstitutionCategoryListQueryHandler(
        IInstitutionRepository institutionRepository,
        IComplaintCategoryRepository categoryRepository)
    {
        _institutionRepository = institutionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<PublicCategoryDto>?> HandleAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var institution = await _institutionRepository.GetByIdAsync(institutionId, cancellationToken);
        if (institution is null || !institution.IsActive)
        {
            return null;
        }

        var categories = await _categoryRepository.ListForInstitutionAsync(institutionId, cancellationToken);

        return categories
            .Select(category => new PublicCategoryDto(category.Id, category.Name, category.Code))
            .ToArray();
    }
}
