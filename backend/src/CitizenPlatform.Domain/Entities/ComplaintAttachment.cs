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
        string contentType,
        long sizeInBytes,
        StorageProvider storageProvider,
        string objectKey,
        Guid? uploadedByUserId,
        GeoCoordinate? photoExifLocation)
        : base(id)
    {
        ComplaintId = Guard.AgainstEmpty(complaintId, nameof(complaintId));
        FileName = Guard.AgainstEmpty(fileName, nameof(fileName), 255);
        ContentType = Guard.AgainstEmpty(contentType, nameof(contentType), 128);
        SizeInBytes = Guard.AgainstNegative(sizeInBytes, nameof(sizeInBytes));
        StorageProvider = storageProvider;
        ObjectKey = Guard.AgainstEmpty(objectKey, nameof(objectKey), 1024);
        UploadedByUserId = uploadedByUserId == Guid.Empty ? null : uploadedByUserId;
        PhotoExifLocation = photoExifLocation;
        PhotoExifGeometry = photoExifLocation?.ToWktPoint();
    }

    public Guid ComplaintId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeInBytes { get; private set; }

    public StorageProvider StorageProvider { get; private set; }

    public string ObjectKey { get; private set; } = string.Empty;

    public Guid? UploadedByUserId { get; private set; }

    public GeoCoordinate? PhotoExifLocation { get; private set; }

    public string? PhotoExifGeometry { get; private set; }

    public static ComplaintAttachment Create(
        Guid complaintId,
        string fileName,
        string contentType,
        long sizeInBytes,
        StorageProvider storageProvider,
        string objectKey,
        Guid? uploadedByUserId = null,
        GeoCoordinate? photoExifLocation = null)
    {
        return new ComplaintAttachment(
            Guid.NewGuid(),
            complaintId,
            fileName,
            contentType,
            sizeInBytes,
            storageProvider,
            objectKey,
            uploadedByUserId,
            photoExifLocation);
    }
}
