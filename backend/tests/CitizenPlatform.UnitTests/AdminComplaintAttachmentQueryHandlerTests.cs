using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class AdminComplaintAttachmentQueryHandlerTests
{
    private static readonly Guid OwnMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task HandleAsync_WhenAttachmentBelongsToOwnComplaint_ReturnsStoredFile()
    {
        var complaint = CreateComplaintWithAttachment(OwnMunicipalityId);
        var attachment = Assert.Single(complaint.Attachments);
        var repository = new FakeRepository(complaint);
        var storage = new FakeStorageService([1, 2, 3]);
        var handler = new AdminComplaintAttachmentQueryHandler(repository, storage);

        var result = await handler.HandleAsync(
            complaint.Id,
            attachment.Id,
            Scope(UserType.MunicipalityEmployee, OwnMunicipalityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("image/jpeg", result!.ContentType);
        Assert.Equal("kanıt.jpg", result.OriginalFileName);
        Assert.Equal(attachment.ObjectKey, storage.LastObjectKey);
        await result.Content.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_WhenComplaintIsOutsideTenant_ReturnsNullWithoutOpeningStorage()
    {
        var complaint = CreateComplaintWithAttachment(OtherMunicipalityId);
        var attachment = Assert.Single(complaint.Attachments);
        var storage = new FakeStorageService([1]);
        var handler = new AdminComplaintAttachmentQueryHandler(new FakeRepository(complaint), storage);

        var result = await handler.HandleAsync(
            complaint.Id,
            attachment.Id,
            Scope(UserType.MunicipalityEmployee, OwnMunicipalityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(storage.LastObjectKey);
    }

    [Fact]
    public async Task HandleAsync_WhenAttachmentBelongsToAnotherComplaint_ReturnsNull()
    {
        var complaint = CreateComplaintWithAttachment(OwnMunicipalityId);
        var storage = new FakeStorageService([1]);
        var handler = new AdminComplaintAttachmentQueryHandler(new FakeRepository(complaint), storage);

        var result = await handler.HandleAsync(
            complaint.Id,
            Guid.NewGuid(),
            Scope(UserType.MunicipalityEmployee, OwnMunicipalityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(storage.LastObjectKey);
    }

    [Fact]
    public async Task HandleAsync_WhenStoredFileIsMissing_ReturnsNull()
    {
        var complaint = CreateComplaintWithAttachment(OwnMunicipalityId);
        var attachment = Assert.Single(complaint.Attachments);
        var handler = new AdminComplaintAttachmentQueryHandler(
            new FakeRepository(complaint),
            new MissingFileStorageService());

        var result = await handler.HandleAsync(
            complaint.Id,
            attachment.Id,
            Scope(UserType.MunicipalityEmployee, OwnMunicipalityId),
            CancellationToken.None);

        Assert.Null(result);
    }

    private static Complaint CreateComplaintWithAttachment(Guid municipalityId)
    {
        var complaint = Complaint.Create(
            municipalityId,
            Guid.NewGuid(),
            "BLD-2026-ATTACH",
            "Fotoğraflı bildirim",
            "Kaldırım hasarı.",
            new GeoCoordinate(41.05, 29.00),
            ComplaintSource.CitizenWeb);

        complaint.AddAttachment(
            "generated.jpg",
            "kanıt.jpg",
            "image/jpeg",
            3,
            new string('a', 64),
            StorageProvider.Local,
            "complaint-id/generated.jpg");

        return complaint;
    }

    private static TenantScope Scope(UserType userType, Guid? municipalityId)
    {
        return TenantScope.From(new FakeCurrentUserService(userType, municipalityId));
    }

    private sealed class FakeRepository : IAdminComplaintQueryRepository
    {
        private readonly Complaint _complaint;

        public FakeRepository(Complaint complaint) => _complaint = complaint;

        public Task<AdminComplaintPagedRows> SearchAsync(AdminComplaintSearchCriteria criteria, CancellationToken cancellationToken)
            => Task.FromResult(new AdminComplaintPagedRows([], criteria.Page, criteria.PageSize, 0));

        public Task<AdminComplaintDetailRow?> GetDetailAsync(
            Guid complaintId,
            Guid? tenantMunicipalityId,
            CancellationToken cancellationToken)
        {
            if (_complaint.Id != complaintId
                || (tenantMunicipalityId is not null && tenantMunicipalityId != _complaint.MunicipalityId))
            {
                return Task.FromResult<AdminComplaintDetailRow?>(null);
            }

            return Task.FromResult<AdminComplaintDetailRow?>(new AdminComplaintDetailRow(
                _complaint,
                "Demo Belediyesi",
                "Yol ve Kaldırım",
                null,
                null,
                null,
                null,
                new Dictionary<Guid, string>()));
        }
    }

    private sealed class FakeStorageService : IFileStorageService
    {
        private readonly byte[] _content;

        public FakeStorageService(byte[] content) => _content = content;

        public string? LastObjectKey { get; private set; }

        public Task<Result<FileStorageSaveResult>> SaveAsync(FileStorageSaveRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
        {
            LastObjectKey = objectKey;
            return Task.FromResult<Stream>(new MemoryStream(_content, writable: false));
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
            => throw new NotSupportedException();
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
        public Guid? InstitutionId { get; }
        public UserType? UserType { get; }
        public IReadOnlyCollection<string> Roles => [];
        public bool IsSystemAdmin => UserType == CitizenPlatform.Domain.Enums.UserType.SystemAdmin;
    }

    private sealed class MissingFileStorageService : IFileStorageService
    {
        public Task<Result<FileStorageSaveResult>> SaveAsync(FileStorageSaveRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
            => throw new FileNotFoundException("Test attachment is missing.", objectKey);

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
