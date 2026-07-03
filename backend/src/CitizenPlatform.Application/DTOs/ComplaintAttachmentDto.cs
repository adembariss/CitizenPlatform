namespace CitizenPlatform.Application.DTOs;

public sealed record ComplaintAttachmentDto(
    Guid AttachmentId,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    string Sha256Hash,
    double? PhotoExifLatitude,
    double? PhotoExifLongitude,
    DateTimeOffset? PhotoTakenAt);
