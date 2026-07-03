namespace CitizenPlatform.Application.DTOs;

public sealed record ComplaintAttachmentUpload(
    string FileName,
    string ContentType,
    long Length,
    Func<Stream> OpenReadStream);
