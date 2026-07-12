using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CitizenPlatform.Infrastructure.Storage;

public sealed class LocalFileStorageService : FileStorageServiceBase
{
    private readonly string _rootPath;

    public LocalFileStorageService(
        IOptions<ObjectStorageOptions> options,
        IFileSafetyScanner fileSafetyScanner)
        : base(options.Value, fileSafetyScanner)
    {
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(options.Value.LocalRootPath)
            ? "storage/complaint-attachments"
            : options.Value.LocalRootPath);

        Directory.CreateDirectory(_rootPath);
    }

    protected override StorageProvider Provider => StorageProvider.Local;

    protected override async Task PersistAsync(
        string objectKey,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var destinationPath = GetSafeLocalPath(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await File.WriteAllBytesAsync(destinationPath, content, cancellationToken);
    }

    public override Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var localPath = GetSafeLocalPath(objectKey);
        Stream stream = new FileStream(
            localPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public override Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var localPath = GetSafeLocalPath(objectKey);
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }

        return Task.CompletedTask;
    }

    private string GetSafeLocalPath(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)
            || Path.IsPathRooted(objectKey)
            || objectKey.Contains("..", StringComparison.Ordinal)
            || objectKey.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid object key.", nameof(objectKey));
        }

        var normalizedObjectKey = objectKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedObjectKey));
        var rootWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid object key.", nameof(objectKey));
        }

        return fullPath;
    }
}
