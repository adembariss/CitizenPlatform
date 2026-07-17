using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class AddAdminCommentCommandHandlerTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ActingUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_WhenValid_AddsCommentWithRequestedVisibility(bool isInternal)
    {
        var complaint = CreateComplaint();
        var outboxRepository = new FakeOutboxRepository();
        var handler = CreateHandler(complaint, outboxRepository);

        var command = new AddAdminCommentCommand(complaint.Id, "Ekip yonlendirildi.", isInternal, ActingUserId);
        var result = await handler.HandleAsync(command, OwnMunicipalityScope(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var comment = Assert.Single(complaint.Comments);
        Assert.Equal(isInternal, comment.IsInternal);
        Assert.Equal("Ekip yonlendirildi.", comment.Body);
        var outboxMessage = Assert.Single(outboxRepository.Items);
        Assert.Equal("AdminCommentAdded", outboxMessage.MessageType);
    }

    [Fact]
    public async Task HandleAsync_WhenCommentTextIsEmpty_ReturnsFailure()
    {
        var complaint = CreateComplaint();
        var handler = CreateHandler(complaint, new FakeOutboxRepository());

        var command = new AddAdminCommentCommand(complaint.Id, "   ", false, ActingUserId);
        var result = await handler.HandleAsync(command, OwnMunicipalityScope(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(complaint.Comments);
    }

    [Fact]
    public async Task HandleAsync_WhenComplaintOutsideTenantScope_ReturnsNotFound()
    {
        var complaint = CreateComplaint();
        var handler = CreateHandler(complaint, new FakeOutboxRepository());

        var command = new AddAdminCommentCommand(complaint.Id, "Not.", false, ActingUserId);
        var result = await handler.HandleAsync(command, OtherMunicipalityScope(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.NotFound);
    }

    private static AddAdminCommentCommandHandler CreateHandler(Complaint complaint, IIntegrationOutboxRepository outboxRepository)
    {
        return new AddAdminCommentCommandHandler(
            new FakeComplaintRepository(complaint),
            outboxRepository,
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());
    }

    private static Complaint CreateComplaint()
    {
        return Complaint.Create(
            MunicipalityId,
            Guid.NewGuid(),
            "BLD-2026-A8F21C",
            "Kaldirim hasari",
            "Mahalle girisindeki kaldirim hasarli.",
            new GeoCoordinate(41.05, 29.00),
            ComplaintSource.CitizenWeb);
    }

    private static TenantScope OwnMunicipalityScope()
    {
        return TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityEmployee, MunicipalityId));
    }

    private static TenantScope OtherMunicipalityScope()
    {
        return TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityEmployee, OtherMunicipalityId));
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        private readonly Complaint _complaint;

        public FakeComplaintRepository(Complaint complaint)
        {
            _complaint = complaint;
        }

        public Task AddAsync(Complaint complaint, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_complaint.Id == id ? _complaint : null);
        }

        public Task<Complaint?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
        {
            return Task.FromResult<Complaint?>(null);
        }

        public Task<bool> ExistsByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class FakeOutboxRepository : IIntegrationOutboxRepository
    {
        public List<IntegrationOutboxMessage> Items { get; } = [];

        public Task AddAsync(IntegrationOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            Items.Add(outboxMessage);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IntegrationOutboxMessage>> GetDueAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<IntegrationOutboxMessage>>(Items);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            return await operation(cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(UserType userType, Guid? municipalityId)
        {
            UserType = userType;
            MunicipalityId = municipalityId;
        }

        public string? UserId => ActingUserId.ToString();

        public Guid? UserGuid => ActingUserId;

        public bool IsAuthenticated => true;

        public Guid? MunicipalityId { get; }
        public Guid? InstitutionId { get; }

        public UserType? UserType { get; }

        public IReadOnlyCollection<string> Roles => [];

        public bool IsSystemAdmin => UserType == CitizenPlatform.Domain.Enums.UserType.SystemAdmin;
    }
}
