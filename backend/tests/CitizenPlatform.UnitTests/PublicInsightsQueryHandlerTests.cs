using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.PublicInsights;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class PublicInsightsQueryHandlerTests
{
    [Fact]
    public async Task StatsHandler_MapsRepositoryRowToDto()
    {
        var repository = new FakePublicInsightsRepository(
            new PublicStatsRow(42, 15, 3, 7));
        var handler = new PublicStatsQueryHandler(repository);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(42, result.TotalComplaints);
        Assert.Equal(15, result.ResolvedComplaints);
        Assert.Equal(3, result.ActiveMunicipalities);
        Assert.Equal(7, result.Categories);
    }

    [Fact]
    public async Task MapHandler_WhenNoLimit_UsesDefaultLimit()
    {
        var repository = new FakePublicInsightsRepository();
        var handler = new PublicComplaintMapQueryHandler(repository);

        await handler.HandleAsync(municipalityId: null, limit: null, CancellationToken.None);

        Assert.Equal(PublicComplaintMapQueryHandler.DefaultLimit, repository.LastLimitRequested);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task MapHandler_WhenLimitIsNonPositive_UsesDefaultLimit(int limit)
    {
        var repository = new FakePublicInsightsRepository();
        var handler = new PublicComplaintMapQueryHandler(repository);

        await handler.HandleAsync(municipalityId: null, limit, CancellationToken.None);

        Assert.Equal(PublicComplaintMapQueryHandler.DefaultLimit, repository.LastLimitRequested);
    }

    [Fact]
    public async Task MapHandler_ClampsLimitToMaximum()
    {
        var repository = new FakePublicInsightsRepository();
        var handler = new PublicComplaintMapQueryHandler(repository);

        await handler.HandleAsync(municipalityId: null, limit: 99999, CancellationToken.None);

        Assert.Equal(PublicComplaintMapQueryHandler.MaxLimit, repository.LastLimitRequested);
    }

    [Fact]
    public async Task MapHandler_MapsRowsAndForwardsMunicipalityFilter()
    {
        var municipalityId = Guid.NewGuid();
        var repository = new FakePublicInsightsRepository(
            mapPoints: [new PublicComplaintMapPointRow(41.05, 29.0, "Yol ve Kaldirim", ComplaintStatus.InProgress, DateTimeOffset.UtcNow)]);
        var handler = new PublicComplaintMapQueryHandler(repository);

        var result = await handler.HandleAsync(municipalityId, limit: 50, CancellationToken.None);

        Assert.Equal(municipalityId, repository.LastMunicipalityIdRequested);
        var point = Assert.Single(result);
        Assert.Equal(41.05, point.Latitude);
        Assert.Equal("Yol ve Kaldirim", point.CategoryName);
        Assert.Equal(ComplaintStatus.InProgress, point.Status);
    }

    private sealed class FakePublicInsightsRepository : IPublicInsightsRepository
    {
        private readonly PublicStatsRow _stats;
        private readonly IReadOnlyList<PublicComplaintMapPointRow> _mapPoints;

        public FakePublicInsightsRepository(
            PublicStatsRow? stats = null,
            IReadOnlyList<PublicComplaintMapPointRow>? mapPoints = null)
        {
            _stats = stats ?? new PublicStatsRow(0, 0, 0, 0);
            _mapPoints = mapPoints ?? [];
        }

        public int? LastLimitRequested { get; private set; }

        public Guid? LastMunicipalityIdRequested { get; private set; }

        public Task<PublicStatsRow> GetStatsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_stats);
        }

        public Task<IReadOnlyList<PublicComplaintMapPointRow>> GetMapPointsAsync(
            Guid? municipalityId,
            int limit,
            CancellationToken cancellationToken)
        {
            LastLimitRequested = limit;
            LastMunicipalityIdRequested = municipalityId;
            return Task.FromResult(_mapPoints);
        }
    }
}
