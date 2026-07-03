using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Domain.Entities;

public sealed class ComplaintAttachment : AuditableEntity
{
    private ComplaintAttachment()
    {
    }

    private ComplaintAttachment(
        Guid id,
        Guid complaintId,
        string fileName,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string sha256Hash,
        StorageProvider storageProvider,
        string objectKey,
        Guid? uploadedByUserId,
        GeoCoordinate? photoExifLocation,
        DateTimeOffset? photoTakenAt)
        : base(id)
    {
        ComplaintId = Guard.AgainstEmpty(complaintId, nameof(complaintId));
        FileName = Guard.AgainstEmpty(fileName, nameof(fileName), 255);
        OriginalFileName = Guard.AgainstEmpty(originalFileName, nameof(originalFileName), 255);
        ContentType = Guard.AgainstEmpty(contentType, nameof(contentType), 128);
        SizeInBytes = Guard.AgainstNegative(sizeInBytes, nameof(sizeInBytes));
        Sha256Hash = Guard.AgainstEmpty(sha256Hash, nameof(sha256Hash), 64);
        StorageProvider = storageProvider;
        ObjectKey = Guard.AgainstEmpty(objectKey, nameof(objectKey), 1024);
        UploadedByUserId = uploadedByUserId == Guid.Empty ? null : uploadedByUserId;
        PhotoExifLocation = photoExifLocation;
        PhotoExifGeometry = photoExifLocation?.ToWktPoint();
        PhotoTakenAt = photoTakenAt;
    }

    public Guid ComplaintId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeInBytes { get; private set; }

    public string Sha256Hash { get; private set; } = string.Empty;

    public StorageProvider StorageProvider { get; private set; }

    public string ObjectKey { get; private set; } = string.Empty;

    public Guid? UploadedByUserId { get; private set; }

    public GeoCoordinate? PhotoExifLocation { get; private set; }

    public string? PhotoExifGeometry { get; private set; }

    public DateTimeOffset? PhotoTakenAt { get; private set; }

    public static ComplaintAttachment Create(
        Guid complaintId,
        string fileName,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string sha256Hash,
        StorageProvider storageProvider,
        string objectKey,
        Guid? uploadedByUserId = null,
        GeoCoordinate? photoExifLocation = null,
        DateTimeOffset? photoTakenAt = null)
    {
        return new ComplaintAttachment(
            Guid.NewGuid(),
            complaintId,
            fileName,
            originalFileName,
            contentType,
            sizeInBytes,
            sha256Hash,
            storageProvider,
            objectKey,
            uploadedByUserId,
            photoExifLocation,
            photoTakenAt);
    }
}
