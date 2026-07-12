using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.PublicInsights;

public sealed class PublicStatsQueryHandler
{
    private readonly IPublicInsightsRepository _repository;

    public PublicStatsQueryHandler(IPublicInsightsRepository repository)
    {
        _repository = repository;
    }

    public async Task<PublicStatsDto> HandleAsync(CancellationToken cancellationToken)
    {
        var row = await _repository.GetStatsAsync(cancellationToken);

        return new PublicStatsDto(
            row.TotalComplaints,
            row.ResolvedComplaints,
            row.ActiveMunicipalities,
            row.Categories);
    }
}
