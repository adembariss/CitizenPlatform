namespace CitizenPlatform.Infrastructure.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string Provider { get; init; } = "Local";

    public string LocalRootPath { get; init; } = "storage/complaint-attachments";

    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public long MaxRequestBodySizeBytes { get; init; } = 50 * 1024 * 1024;

    public string[] AllowedContentTypes { get; init; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public string Endpoint { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public bool UseSsl { get; init; }
}
