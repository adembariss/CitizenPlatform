using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class AdminComplaintListQueryHandlerTests
{
    private static readonly Guid OwnMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task HandleAsync_WhenMunicipalityEmployeeRequestsAnotherMunicipality_QueryIsForcedToOwnMunicipality()
    {
        var repository = new FakeAdminComplaintQueryRepository();
        var handler = new AdminComplaintListQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityEmployee, OwnMunicipalityId));

        var query = new AdminComplaintListQuery(null, null, null, null, null, null, 1, 20, OtherMunicipalityId);
        await handler.HandleAsync(query, scope, CancellationToken.None);

        Assert.NotNull(repository.LastCriteria);
        Assert.Equal(OwnMunicipalityId, repository.LastCriteria!.MunicipalityId);
    }

    [Fact]
    public async Task HandleAsync_WhenSystemAdminRequestsNoFilter_SearchesAllMunicipalities()
    {
        var repository = new FakeAdminComplaintQueryRepository();
        var handler = new AdminComplaintListQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));

        var query = new AdminComplaintListQuery(null, null, null, null, null, null, 1, 20, null);
        await handler.HandleAsync(query, scope, CancellationToken.None);

        Assert.NotNull(repository.LastCriteria);
        Assert.Null(repository.LastCriteria!.MunicipalityId);
    }

    [Fact]
    public async Task HandleAsync_MasksCitizenFullNameInResponse()
    {
        var repository = new FakeAdminComplaintQueryRepository();
        var handler = new AdminComplaintListQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));

        var query = new AdminComplaintListQuery(null, null, null, null, null, null, 1, 20, null);
        var response = await handler.HandleAsync(query, scope, CancellationToken.None);

        var item = Assert.Single(response.Items);
        Assert.Equal("A*** L***", item.CitizenFullName);
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSizeToMaximum()
    {
        var repository = new FakeAdminComplaintQueryRepository();
        var handler = new AdminComplaintListQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));

        var query = new AdminComplaintListQuery(null, null, null, null, null, null, 1, 500, null);
        await handler.HandleAsync(query, scope, CancellationToken.None);

        Assert.Equal(100, repository.LastCriteria!.PageSize);
    }

    private sealed class FakeAdminComplaintQueryRepository : IAdminComplaintQueryRepository
    {
        public AdminComplaintSearchCriteria? LastCriteria { get; private set; }

        public Task<AdminComplaintPagedRows> SearchAsync(AdminComplaintSearchCriteria criteria, CancellationToken cancellationToken)
        {
            LastCriteria = criteria;

            var row = new AdminComplaintListRow(
                Guid.NewGuid(),
                "BLD-2026-A8F21C",
                "Demo Belediyesi",
                "Yol ve Kaldirim",
                "Fen Isleri",
                "Kaldirim hasari",
                "Mahalle girisindeki kaldirim hasarli.",
                ComplaintStatus.New,
                ComplaintPriority.Normal,
                "Ada Lovelace",
                "Demo adres",
                41.05,
                29.00,
                DateTimeOffset.UtcNow,
                null);

            return Task.FromResult(new AdminComplaintPagedRows([row], criteria.Page, criteria.PageSize, 1));
        }

        public Task<AdminComplaintDetailRow?> GetDetailAsync(Guid complaintId, Guid? tenantMunicipalityId, CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminComplaintDetailRow?>(null);
        }
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
