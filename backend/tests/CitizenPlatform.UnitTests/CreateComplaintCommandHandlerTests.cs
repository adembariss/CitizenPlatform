using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using FluentValidation;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class CreateComplaintCommandHandlerTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    private static readonly Guid DepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333301");

    [Fact]
    public async Task HandleAsync_WhenCoordinateIsInsideBoundary_CreatesComplaint()
    {
        var context = HandlerContext.Create();
        var command = CreateValidCommand();

        var result = await context.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value!.ComplaintId);
        Assert.Equal("BLD-2026-A8F21C", result.Value.TrackingCode);
        Assert.Equal("Demo Belediyesi", result.Value.MunicipalityName);
        Assert.Equal(ComplaintStatus.New, result.Value.Status);
        Assert.Single(context.ComplaintRepository.Items);
        Assert.Single(context.ComplaintRepository.Items[0].StatusHistories);
        Assert.Equal(DepartmentId, context.ComplaintRepository.Items[0].CurrentDepartmentId);
    }

    [Fact]
    public async Task HandleAsync_WhenCoordinateIsOutsideBoundary_ReturnsFailure()
    {
        var context = HandlerContext.Create(resolveSuccess: false);

        var result = await context.Handler.HandleAsync(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(context.ComplaintRepository.Items);
        Assert.Contains("No active municipality", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryIsInactive_ReturnsFailure()
    {
        var context = HandlerContext.Create(categoryAvailable: false);

        var result = await context.Handler.HandleAsync(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(context.ComplaintRepository.Items);
        Assert.Contains("category", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_WhenComplaintCreated_AddsOutboxMessage()
    {
        var context = HandlerContext.Create();

        await context.Handler.HandleAsync(CreateValidCommand(), CancellationToken.None);

        var outboxMessage = Assert.Single(context.OutboxRepository.Items);
        Assert.Equal("ComplaintCreated", outboxMessage.MessageType);
        Assert.Equal(MunicipalityId, outboxMessage.MunicipalityId);
    }

    [Fact]
    public async Task HandleAsync_WhenComplaintCreated_GeneratesTrackingCode()
    {
        var context = HandlerContext.Create();

        var result = await context.Handler.HandleAsync(CreateValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.TrackingCode));
        Assert.StartsWith("BLD-2026-", result.Value.TrackingCode, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenAttachmentProvided_AddsAttachment()
    {
        var context = HandlerContext.Create();

        var result = await context.Handler.HandleAsync(
            CreateValidCommand([CreateJpegUpload()]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var complaint = Assert.Single(context.ComplaintRepository.Items);
        var attachment = Assert.Single(complaint.Attachments);
        Assert.Equal("photo.jpg", attachment.OriginalFileName);
        Assert.Equal("image/jpeg", attachment.ContentType);
        Assert.False(string.IsNullOrWhiteSpace(attachment.Sha256Hash));
    }

    private static CreateComplaintCommand CreateValidCommand(
        IReadOnlyCollection<ComplaintAttachmentUpload>? attachments = null)
    {
        return new CreateComplaintCommand(
            CategoryId,
            null,
            "Mahalle girişindeki kaldırım hasarlı.",
            "Ada Lovelace",
            "+905551112233",
            "ada@example.com",
            41.05,
            29.00,
            "Demo adres",
            false,
            ComplaintSource.CitizenWeb,
            attachments);
    }

    private static ComplaintAttachmentUpload CreateJpegUpload()
    {
        return new ComplaintAttachmentUpload(
            "photo.jpg",
            "image/jpeg",
            4,
            () => new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]));
    }

    private sealed record HandlerContext(
        CreateComplaintCommandHandler Handler,
        FakeComplaintRepository ComplaintRepository,
        FakeIntegrationOutboxRepository OutboxRepository)
    {
        public static HandlerContext Create(bool resolveSuccess = true, bool categoryAvailable = true)
        {
            var complaintRepository = new FakeComplaintRepository();
            var outboxRepository = new FakeIntegrationOutboxRepository();

            var handler = new CreateComplaintCommandHandler(
                new CreateComplaintCommandValidator(),
                new FakeGeoMunicipalityResolver(resolveSuccess),
                new FakeComplaintCategoryRepository(categoryAvailable),
                new FakeDepartmentRepository(),
                new FakeCategoryDepartmentRuleRepository(),
                new FakeCitizenRepository(),
                complaintRepository,
                outboxRepository,
                new ComplaintAttachmentUploadService(new FakeFileStorageService(), new FakeImageMetadataReader()),
                new FakeTrackingCodeGenerator(),
                new FakeDateTimeProvider(),
                new FakeUnitOfWork());

            return new HandlerContext(handler, complaintRepository, outboxRepository);
        }
    }

    private sealed class FakeGeoMunicipalityResolver : IGeoMunicipalityResolver
    {
        private readonly bool _success;

        public FakeGeoMunicipalityResolver(bool success)
        {
            _success = success;
        }

        public Task<MunicipalityResolveResult> ResolveByCoordinateAsync(double latitude, double longitude, CancellationToken ct)
        {
            var result = _success
                ? MunicipalityResolveResult.Success(MunicipalityId, "Demo Belediyesi", "DEMO")
                : MunicipalityResolveResult.Failure("No active municipality boundary contains the coordinate.");

            return Task.FromResult(result);
        }
    }

    private sealed class FakeComplaintCategoryRepository : IComplaintCategoryRepository
    {
        private readonly bool _categoryAvailable;

        public FakeComplaintCategoryRepository(bool categoryAvailable)
        {
            _categoryAvailable = categoryAvailable;
        }

        public Task<ComplaintCategory?> GetActiveForMunicipalityAsync(Guid categoryId, Guid municipalityId, CancellationToken cancellationToken)
        {
            ComplaintCategory? category = _categoryAvailable
                ? ComplaintCategory.Create("Yol ve Kaldırım", "YOL_KALDIRIM", municipalityId)
                : null;

            return Task.FromResult(category);
        }
    }

    private sealed class FakeCategoryDepartmentRuleRepository : ICategoryDepartmentRuleRepository
    {
        public Task<CategoryDepartmentRule?> GetActiveRuleAsync(Guid municipalityId, Guid categoryId, CancellationToken cancellationToken)
        {
            CategoryDepartmentRule? rule = CategoryDepartmentRule.Create(
                municipalityId,
                categoryId,
                DepartmentId,
                ComplaintPriority.Normal);

            return Task.FromResult<CategoryDepartmentRule?>(rule);
        }
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Department? department = Department.Create(MunicipalityId, "Fen Isleri", "FEN_ISLERI");
            return Task.FromResult<Department?>(department);
        }
    }

    private sealed class FakeCitizenRepository : ICitizenRepository
    {
        public Task AddAsync(Citizen citizen, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        public List<Complaint> Items { get; } = [];

        public Task AddAsync(Complaint complaint, CancellationToken cancellationToken)
        {
            Items.Add(complaint);
            return Task.CompletedTask;
        }

        public Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(complaint => complaint.Id == id));
        }

        public Task<Complaint?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(complaint => complaint.TrackingCode == trackingCode));
        }

        public Task<bool> ExistsByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class FakeIntegrationOutboxRepository : IIntegrationOutboxRepository
    {
        public List<IntegrationOutboxMessage> Items { get; } = [];

        public Task AddAsync(IntegrationOutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            Items.Add(outboxMessage);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IntegrationOutboxMessage>> GetDueAsync(
            DateTimeOffset utcNow,
            int batchSize,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<IntegrationOutboxMessage>>(Items);
        }
    }

    private sealed class FakeTrackingCodeGenerator : ITrackingCodeGenerator
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult("BLD-2026-A8F21C");
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 06, 24, 12, 00, 00, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            return await operation(cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<Result<FileStorageSaveResult>> SaveAsync(
            FileStorageSaveRequest request,
            CancellationToken cancellationToken)
        {
            var result = new FileStorageSaveResult(
                "complaint-attachments/test.jpg",
                "test.jpg",
                request.OriginalFileName,
                request.ContentType,
                request.SizeInBytes,
                new string('a', 64),
                StorageProvider.Local);

            return Task.FromResult(Result<FileStorageSaveResult>.Success(result));
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]));
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeImageMetadataReader : IImageMetadataReader
    {
        public Task<ImageMetadata> ReadAsync(Stream content, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImageMetadata.Empty);
        }
    }
}
