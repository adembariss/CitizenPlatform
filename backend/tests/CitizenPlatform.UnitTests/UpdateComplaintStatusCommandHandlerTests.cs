using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class UpdateComplaintStatusCommandHandlerTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ActingUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task HandleAsync_WhenValid_CreatesStatusHistoryAndOutboxEvent()
    {
        var complaint = CreateComplaint();
        var complaintRepository = new FakeComplaintRepository(complaint);
        var outboxRepository = new FakeOutboxRepository();
        var handler = CreateHandler(complaintRepository, outboxRepository);

        var command = new UpdateComplaintStatusCommand(complaint.Id, ComplaintStatus.UnderReview, "Inceleniyor.", true, ActingUserId);
        var result = await handler.HandleAsync(command, OwnMunicipalityScope(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var history = Assert.Single(complaint.StatusHistories, h => h.NewStatus == ComplaintStatus.UnderReview);
        Assert.Equal(ComplaintStatus.New, history.PreviousStatus);
        Assert.True(history.IsVisibleToCitizen);
        var outboxMessage = Assert.Single(outboxRepository.Items);
        Assert.Equal("ComplaintStatusChanged", outboxMessage.MessageType);
    }

    [Fact]
    public async Task HandleAsync_WhenStatusResolvedOrClosed_SetsClosedAt()
    {
        var complaint = CreateComplaint();
        var handler = CreateHandler(new FakeComplaintRepository(complaint), new FakeOutboxRepository());

        var command = new UpdateComplaintStatusCommand(complaint.Id, ComplaintStatus.Resolved, null, true, ActingUserId);
        await handler.HandleAsync(command, OwnMunicipalityScope(), CancellationToken.None);

        Assert.NotNull(complaint.ClosedAt);
    }

    [Fact]
    public async Task HandleAsync_WhenSameStatusRequested_ReturnsFailureWithoutSideEffects()
    {
        var complaint = CreateComplaint();
        var outboxRepository = new FakeOutboxRepository();
        var handler = CreateHandler(new FakeComplaintRepository(complaint), outboxRepository);

        var command = new UpdateComplaintStatusCommand(complaint.Id, ComplaintStatus.New, null, true, ActingUserId);
        var result = await handler.HandleAsync(command, OwnMunicipalityScope(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(result.NotFound);
        Assert.Empty(outboxRepository.Items);
    }

    [Fact]
    public async Task HandleAsync_WhenComplaintBelongsToAnotherMunicipality_ReturnsNotFound()
    {
        var complaint = CreateComplaint();
        var handler = CreateHandler(new FakeComplaintRepository(complaint), new FakeOutboxRepository());

        var command = new UpdateComplaintStatusCommand(complaint.Id, ComplaintStatus.UnderReview, null, true, ActingUserId);
        var result = await handler.HandleAsync(command, OtherMunicipalityScope(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenSystemAdmin_CanUpdateAnyMunicipalityComplaint()
    {
        var complaint = CreateComplaint();
        var handler = CreateHandler(new FakeComplaintRepository(complaint), new FakeOutboxRepository());

        var command = new UpdateComplaintStatusCommand(complaint.Id, ComplaintStatus.UnderReview, null, true, ActingUserId);
        var result = await handler.HandleAsync(command, SystemAdminScope(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private static UpdateComplaintStatusCommandHandler CreateHandler(
        IComplaintRepository complaintRepository,
        IIntegrationOutboxRepository outboxRepository)
    {
        return new UpdateComplaintStatusCommandHandler(
            complaintRepository,
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

    private static TenantScope SystemAdminScope()
    {
        return TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, null));
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

        public UserType? UserType { get; }

        public IReadOnlyCollection<string> Roles => [];

        public bool IsSystemAdmin => UserType == CitizenPlatform.Domain.Enums.UserType.SystemAdmin;
    }
}
