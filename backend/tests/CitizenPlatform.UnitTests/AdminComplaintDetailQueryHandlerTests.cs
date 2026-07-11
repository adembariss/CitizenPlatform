using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class AdminComplaintDetailQueryHandlerTests
{
    private static readonly Guid OwnMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task HandleAsync_WhenMunicipalityEmployee_RequestsComplaintFromAnotherMunicipality_ReturnsNull()
    {
        var complaint = CreateComplaint(OtherMunicipalityId);
        var repository = new FakeAdminComplaintQueryRepository(complaint);
        var handler = new AdminComplaintDetailQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityEmployee, OwnMunicipalityId));

        var detail = await handler.HandleAsync(complaint.Id, scope, CancellationToken.None);

        Assert.Null(detail);
        Assert.Equal(OwnMunicipalityId, repository.LastTenantMunicipalityIdRequested);
    }

    [Fact]
    public async Task HandleAsync_WhenMunicipalityEmployee_RequestsOwnComplaint_ReturnsDetail()
    {
        var complaint = CreateComplaint(OwnMunicipalityId);
        var repository = new FakeAdminComplaintQueryRepository(complaint);
        var handler = new AdminComplaintDetailQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityEmployee, OwnMunicipalityId));

        var detail = await handler.HandleAsync(complaint.Id, scope, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(complaint.Id, detail!.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenSystemAdmin_CanSeeAnyMunicipalityComplaint()
    {
        var complaint = CreateComplaint(OtherMunicipalityId);
        var repository = new FakeAdminComplaintQueryRepository(complaint);
        var handler = new AdminComplaintDetailQueryHandler(repository);
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));

        var detail = await handler.HandleAsync(complaint.Id, scope, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Null(repository.LastTenantMunicipalityIdRequested);
    }

    private static Complaint CreateComplaint(Guid municipalityId)
    {
        return Complaint.Create(
            municipalityId,
            Guid.NewGuid(),
            "BLD-2026-A8F21C",
            "Kaldirim hasari",
            "Mahalle girisindeki kaldirim hasarli.",
            new GeoCoordinate(41.05, 29.00),
            ComplaintSource.CitizenWeb);
    }

    private sealed class FakeAdminComplaintQueryRepository : IAdminComplaintQueryRepository
    {
        private readonly Complaint _complaint;

        public FakeAdminComplaintQueryRepository(Complaint complaint)
        {
            _complaint = complaint;
        }

        public Guid? LastTenantMunicipalityIdRequested { get; private set; }

        public Task<AdminComplaintPagedRows> SearchAsync(AdminComplaintSearchCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AdminComplaintPagedRows([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<AdminComplaintDetailRow?> GetDetailAsync(Guid complaintId, Guid? tenantMunicipalityId, CancellationToken cancellationToken)
        {
            LastTenantMunicipalityIdRequested = tenantMunicipalityId;

            if (_complaint.Id != complaintId)
            {
                return Task.FromResult<AdminComplaintDetailRow?>(null);
            }

            if (tenantMunicipalityId is not null && _complaint.MunicipalityId != tenantMunicipalityId)
            {
                return Task.FromResult<AdminComplaintDetailRow?>(null);
            }

            var row = new AdminComplaintDetailRow(
                _complaint,
                "Demo Belediyesi",
                "Yol ve Kaldirim",
                null,
                "Ada Lovelace",
                "+905551112233",
                "ada@example.com",
                new Dictionary<Guid, string>());

            return Task.FromResult<AdminComplaintDetailRow?>(row);
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
