using System.Text.Json;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Outbox;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class OutboxProcessingServiceTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ComplaintId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenComplaintCreated_WritesToMunicipalityDbAndCompletesOutbox()
    {
        var outboxMessage = CreateOutboxMessage();
        var writer = new FakeMunicipalityComplaintWriter();
        var service = CreateService([outboxMessage], writer);

        var processedCount = await service.ProcessDueMessagesAsync(CancellationToken.None);

        Assert.Equal(1, processedCount);
        Assert.Equal(OutboxStatus.Completed, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.Single(outboxMessage.Attempts);
        Assert.True(outboxMessage.Attempts.Single().Succeeded);
        Assert.Equal(1, writer.InsertedCount);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenSameComplaintArrivesTwice_DoesNotDuplicateMunicipalityInsert()
    {
        var firstMessage = CreateOutboxMessage();
        var secondMessage = CreateOutboxMessage();
        var writer = new FakeMunicipalityComplaintWriter();
        var service = CreateService([firstMessage, secondMessage], writer);

        var processedCount = await service.ProcessDueMessagesAsync(CancellationToken.None);

        Assert.Equal(2, processedCount);
        Assert.Equal(OutboxStatus.Completed, firstMessage.Status);
        Assert.Equal(OutboxStatus.Completed, secondMessage.Status);
        Assert.Equal(2, writer.WriteCallCount);
        Assert.Equal(1, writer.InsertedCount);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenMunicipalityDbIsUnavailable_KeepsOutboxPendingAndIncrementsRetry()
    {
        var outboxMessage = CreateOutboxMessage();
        var writer = new FakeMunicipalityComplaintWriter("database unavailable");
        var service = CreateService([outboxMessage], writer);

        await service.ProcessDueMessagesAsync(CancellationToken.None);

        Assert.Equal(OutboxStatus.Pending, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.NotNull(outboxMessage.NextRetryAt);
        var attempt = Assert.Single(outboxMessage.Attempts);
        Assert.False(attempt.Succeeded);
        Assert.Contains("database unavailable", attempt.ErrorMessage);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenMaxRetryIsExceeded_MarksOutboxFailed()
    {
        var outboxMessage = CreateOutboxMessage();
        var writer = new FakeMunicipalityComplaintWriter("database unavailable");
        var service = CreateService(
            [outboxMessage],
            writer,
            new OutboxProcessingOptions
            {
                BatchSize = 10,
                MaxRetryCount = 1,
                InitialRetryDelaySeconds = 1,
                MaxRetryDelaySeconds = 10
            });

        await service.ProcessDueMessagesAsync(CancellationToken.None);

        Assert.Equal(OutboxStatus.Failed, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.Null(outboxMessage.NextRetryAt);
        Assert.Contains("database unavailable", outboxMessage.FailureReason);
    }

    private static OutboxProcessingService CreateService(
        IReadOnlyCollection<IntegrationOutboxMessage> messages,
        FakeMunicipalityComplaintWriter writer,
        OutboxProcessingOptions? options = null)
    {
        return new OutboxProcessingService(
            new FakeIntegrationOutboxRepository(messages),
            new FakeMunicipalityDatabaseConnectionResolver(),
            [writer],
            new FakeDateTimeProvider(),
            new FakeUnitOfWork(),
            options ?? new OutboxProcessingOptions
            {
                BatchSize = 10,
                MaxRetryCount = 5,
                InitialRetryDelaySeconds = 1,
                MaxRetryDelaySeconds = 10
            });
    }

    private static IntegrationOutboxMessage CreateOutboxMessage()
    {
        var payload = new MunicipalityComplaintCreatedPayload(
            ComplaintId,
            MunicipalityId,
            "BLD-2026-A8F21C",
            "Yol ve Kaldirim",
            "Fen Isleri",
            "Ada Lovelace",
            "+905551112233",
            "ada@example.com",
            "Mahalle girisindeki kaldirim hasarli.",
            "Demo adres",
            41.05,
            29.00,
            "New",
            "Normal",
            new DateTimeOffset(2026, 7, 12, 10, 0, 0, TimeSpan.Zero));

        return IntegrationOutboxMessage.Create(
            MunicipalityId,
            ComplaintId,
            nameof(Complaint),
            "ComplaintCreated",
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private sealed class FakeIntegrationOutboxRepository : IIntegrationOutboxRepository
    {
        private readonly IReadOnlyCollection<IntegrationOutboxMessage> _messages;

        public FakeIntegrationOutboxRepository(IReadOnlyCollection<IntegrationOutboxMessage> messages)
        {
            _messages = messages;
        }

        public Task AddAsync(IntegrationOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<IntegrationOutboxMessage>> GetDueAsync(
            DateTimeOffset utcNow,
            int batchSize,
            CancellationToken cancellationToken)
        {
            var dueMessages = _messages
                .Where(message => message.Status == OutboxStatus.Pending
                    && (message.NextRetryAt is null || message.NextRetryAt <= utcNow))
                .Take(batchSize)
                .ToArray();

            return Task.FromResult<IReadOnlyList<IntegrationOutboxMessage>>(dueMessages);
        }
    }

    private sealed class FakeMunicipalityDatabaseConnectionResolver : IMunicipalityDatabaseConnectionResolver
    {
        public Task<Result<MunicipalityDatabaseConnectionInfo>> ResolveAsync(
            Guid municipalityId,
            CancellationToken cancellationToken)
        {
            var info = new MunicipalityDatabaseConnectionInfo(
                municipalityId,
                MunicipalityDbProvider.PostgreSql,
                "fake",
                "Host=localhost;Database=fake");

            return Task.FromResult(Result<MunicipalityDatabaseConnectionInfo>.Success(info));
        }
    }

    private sealed class FakeMunicipalityComplaintWriter : IMunicipalityComplaintWriter
    {
        private readonly string? _failure;
        private readonly HashSet<Guid> _insertedComplaintIds = [];

        public FakeMunicipalityComplaintWriter(string? failure = null)
        {
            _failure = failure;
        }

        public MunicipalityDbProvider Provider => MunicipalityDbProvider.PostgreSql;

        public int WriteCallCount { get; private set; }

        public int InsertedCount { get; private set; }

        public Task<Result<MunicipalityComplaintWriteResult>> WriteComplaintCreatedAsync(
            MunicipalityDatabaseConnectionInfo connectionInfo,
            MunicipalityComplaintCreatedPayload payload,
            CancellationToken cancellationToken)
        {
            WriteCallCount++;
            if (_failure is not null)
            {
                return Task.FromResult(Result<MunicipalityComplaintWriteResult>.Failure(_failure));
            }

            var wasInserted = _insertedComplaintIds.Add(payload.ComplaintId);
            if (wasInserted)
            {
                InsertedCount++;
            }

            return Task.FromResult(Result<MunicipalityComplaintWriteResult>.Success(
                new MunicipalityComplaintWriteResult(wasInserted)));
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
