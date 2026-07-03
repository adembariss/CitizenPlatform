using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public sealed record FileStorageSaveResult(
    string ObjectKey,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    string Sha256Hash,
    StorageProvider StorageProvider);
