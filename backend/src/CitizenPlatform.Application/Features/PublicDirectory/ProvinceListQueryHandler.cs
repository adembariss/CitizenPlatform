using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Application.Features.PublicDirectory;

public sealed class ProvinceListQueryHandler
{
    private readonly IMunicipalityRepository _municipalityRepository;

    public ProvinceListQueryHandler(IMunicipalityRepository municipalityRepository)
    {
        _municipalityRepository = municipalityRepository;
    }

    public Task<IReadOnlyList<string>> HandleAsync(CancellationToken cancellationToken)
    {
        return _municipalityRepository.GetProvincesAsync(cancellationToken);
    }
}
