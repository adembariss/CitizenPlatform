namespace CitizenPlatform.Application.Abstractions;

public sealed record FileStorageSaveRequest(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes);
