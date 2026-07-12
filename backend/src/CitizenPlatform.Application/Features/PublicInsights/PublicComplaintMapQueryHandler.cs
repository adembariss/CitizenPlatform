using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.PublicInsights;

public sealed class PublicComplaintMapQueryHandler
{
    public const int DefaultLimit = 300;
    public const int MaxLimit = 1000;

    private readonly IPublicInsightsRepository _repository;

    public PublicComplaintMapQueryHandler(IPublicInsightsRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PublicComplaintMapPointDto>> HandleAsync(
        Guid? municipalityId,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = limit is null or <= 0 ? DefaultLimit : Math.Min(limit.Value, MaxLimit);

        var rows = await _repository.GetMapPointsAsync(municipalityId, take, cancellationToken);

        return rows
            .Select(row => new PublicComplaintMapPointDto(
                row.Latitude,
                row.Longitude,
                row.CategoryName,
                row.Status,
                row.CreatedAt))
            .ToArray();
    }
}
