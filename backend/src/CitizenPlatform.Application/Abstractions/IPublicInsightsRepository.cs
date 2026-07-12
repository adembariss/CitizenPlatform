using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public sealed record PublicStatsRow(
    int TotalComplaints,
    int ResolvedComplaints,
    int ActiveMunicipalities,
    int Categories);

public sealed record PublicComplaintMapPointRow(
    double Latitude,
    double Longitude,
    string CategoryName,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt);

public interface IPublicInsightsRepository
{
    Task<PublicStatsRow> GetStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PublicComplaintMapPointRow>> GetMapPointsAsync(
        Guid? municipalityId,
        int limit,
        CancellationToken cancellationToken);
}
