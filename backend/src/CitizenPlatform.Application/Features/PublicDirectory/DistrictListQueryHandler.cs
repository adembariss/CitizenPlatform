using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.PublicDirectory;

public sealed class DistrictListQueryHandler
{
    private readonly IMunicipalityRepository _municipalityRepository;

    public DistrictListQueryHandler(IMunicipalityRepository municipalityRepository)
    {
        _municipalityRepository = municipalityRepository;
    }

    public async Task<IReadOnlyList<PublicDistrictDto>> HandleAsync(string province, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(province))
        {
            return Array.Empty<PublicDistrictDto>();
        }

        var districts = await _municipalityRepository.GetDistrictsByProvinceAsync(province.Trim(), cancellationToken);

        return districts
            .Select(district => new PublicDistrictDto(
                district.Id,
                district.Name,
                district.Code,
                district.Latitude,
                district.Longitude))
            .ToArray();
    }
}
