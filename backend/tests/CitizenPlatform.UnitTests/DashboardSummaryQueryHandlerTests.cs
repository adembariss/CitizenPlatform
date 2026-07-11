using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Features.AdminDashboard;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class DashboardSummaryQueryHandlerTests
{
    private static readonly Guid OwnMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task HandleAsync_WhenMunicipalityAdmin_AlwaysQueriesOwnMunicipality()
    {
        var repository = new FakeAdminDashboardRepository();
        var handler = new DashboardSummaryQueryHandler(repository, new FakeDateTimeProvider());
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityAdmin, OwnMunicipalityId));

        await handler.HandleAsync(OtherMunicipalityId, scope, CancellationToken.None);

        Assert.Equal(OwnMunicipalityId, repository.LastMunicipalityIdRequested);
    }

    [Fact]
    public async Task HandleAsync_WhenSystemAdminWithNoFilter_QueriesAllMunicipalities()
    {
        var repository = new FakeAdminDashboardRepository();
        var handler = new DashboardSummaryQueryHandler(repository, new FakeDateTimeProvider());
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));

        await handler.HandleAsync(null, scope, CancellationToken.None);

        Assert.Null(repository.LastMunicipalityIdRequested);
    }

    [Fact]
    public async Task HandleAsync_MapsSummaryFieldsCorrectly()
    {
        var repository = new FakeAdminDashboardRepository();
        var handler = new DashboardSummaryQueryHandler(repository, new FakeDateTimeProvider());
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));

        var summary = await handler.HandleAsync(null, scope, CancellationToken.None);

        Assert.Equal(10, summary.TotalComplaints);
        Assert.Single(summary.ByStatus);
        Assert.Equal("New", summary.ByStatus[0].Status);
    }

    private sealed class FakeAdminDashboardRepository : IAdminDashboardRepository
    {
        public Guid? LastMunicipalityIdRequested { get; private set; }

        public Task<DashboardSummaryRow> GetSummaryAsync(Guid? municipalityId, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            LastMunicipalityIdRequested = municipalityId;

            var row = new DashboardSummaryRow(
                10,
                5,
                2,
                3,
                2,
                12.5,
                [new StatusCountRow("New", 10)],
                [],
                []);

            return Task.FromResult(row);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(UserType userType, Guid? municipalityId)
        {
            UserType = userType;
            MunicipalityId = municipalityId;
        }

        public string? UserId => Guid.NewGuid().ToString();

        public Guid? UserGuid => Guid.Parse(UserId!);

        public bool IsAuthenticated => true;

        public Guid? MunicipalityId { get; }

        public UserType? UserType { get; }

        public IReadOnlyCollection<string> Roles => [];

        public bool IsSystemAdmin => UserType == CitizenPlatform.Domain.Enums.UserType.SystemAdmin;
    }
}
