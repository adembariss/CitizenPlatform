using System.Buffers;
using System.Security.Cryptography;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CitizenPlatform.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly string[] DefaultAllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly ObjectStorageOptions _options;
    private readonly IFileSafetyScanner _fileSafetyScanner;
    private readonly string _rootPath;

    public LocalFileStorageService(
        IOptions<ObjectStorageOptions> options,
        IFileSafetyScanner fileSafetyScanner)
    {
        _options = options.Value;
        _fileSafetyScanner = fileSafetyScanner;
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(_options.LocalRootPath)
            ? "storage/complaint-attachments"
            : _options.LocalRootPath);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<Result<FileStorageSaveResult>> SaveAsync(
        FileStorageSaveRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        var normalizedContentType = NormalizeContentType(request.ContentType);
        var allowedContentTypes = GetAllowedContentTypes();
        if (!allowedContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Result<FileStorageSaveResult>.Failure("File content type is not allowed.");
        }

        var maxFileSizeBytes = _options.MaxFileSizeBytes > 0
            ? _options.MaxFileSizeBytes
            : DefaultMaxFileSizeBytes;

        if (request.SizeInBytes <= 0)
        {
            return Result<FileStorageSaveResult>.Failure("File is empty.");
        }

        if (request.SizeInBytes > maxFileSizeBytes)
        {
            return Result<FileStorageSaveResult>.Failure("File exceeds the configured size limit.");
        }

        var readResult = await ReadAllBytesWithinLimitAsync(request.Content, maxFileSizeBytes, cancellationToken);
        if (!readResult.IsSuccess || readResult.Value is null)
        {
            return Result<FileStorageSaveResult>.Failure(readResult.Error ?? "File could not be read.");
        }

        var detectedContentType = DetectContentType(readResult.Value);
        if (detectedContentType is null)
        {
            return Result<FileStorageSaveResult>.Failure("File signature is not a supported image format.");
        }

        if (!string.Equals(normalizedContentType, detectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Result<FileStorageSaveResult>.Failure("File content type does not match the file signature.");
        }

        await using (var scanStream = new MemoryStream(readResult.Value, writable: false))
        {
            var scanResult = await _fileSafetyScanner.ScanAsync(
                scanStream,
                request.OriginalFileName,
                normalizedContentType,
                cancellationToken);

            if (!scanResult.IsSafe)
            {
                return Result<FileStorageSaveResult>.Failure(scanResult.FailureReason ?? "File safety scan failed.");
            }
        }

        var safeOriginalFileName = SanitizeOriginalFileName(request.OriginalFileName);
        var storedFileName = $"{RandomNumberGenerator.GetHexString(16).ToLowerInvariant()}.{GetExtension(detectedContentType)}";
        var now = DateTimeOffset.UtcNow;
        var objectKey = FormattableString.Invariant($"complaint-attachments/{now:yyyy}/{now:MM}/{storedFileName}");
        var destinationPath = GetSafeLocalPath(objectKey);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await File.WriteAllBytesAsync(destinationPath, readResult.Value, cancellationToken);

        var sha256Hash = Convert.ToHexString(SHA256.HashData(readResult.Value)).ToLowerInvariant();
        return Result<FileStorageSaveResult>.Success(new FileStorageSaveResult(
            objectKey,
            storedFileName,
            safeOriginalFileName,
            detectedContentType,
            readResult.Value.LongLength,
            sha256Hash,
            StorageProvider.Local));
    }

    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
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

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var localPath = GetSafeLocalPath(objectKey);
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }

        return Task.CompletedTask;
    }

    private static async Task<Result<byte[]>> ReadAllBytesWithinLimitAsync(
        Stream content,
        long maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            using var memoryStream = new MemoryStream();
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                if (memoryStream.Length + read > maxFileSizeBytes)
                {
                    return Result<byte[]>.Failure("File exceeds the configured size limit.");
                }

                memoryStream.Write(buffer, 0, read);
            }

            return memoryStream.Length == 0
                ? Result<byte[]>.Failure("File is empty.")
                : Result<byte[]>.Success(memoryStream.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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

    private IReadOnlyCollection<string> GetAllowedContentTypes()
    {
        return _options.AllowedContentTypes is { Length: > 0 }
            ? _options.AllowedContentTypes
            : DefaultAllowedContentTypes;
    }

    private static string NormalizeContentType(string contentType)
    {
        return contentType.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.ToLowerInvariant()
            ?? string.Empty;
    }

    private static string SanitizeOriginalFileName(string originalFileName)
    {
        var fileName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "upload";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(character => !char.IsControl(character) && !invalidChars.Contains(character))
            .ToArray())
            .Trim();

        return string.IsNullOrWhiteSpace(sanitized)
            ? "upload"
            : sanitized[..Math.Min(sanitized.Length, 255)];
    }

    private static string? DetectContentType(byte[] content)
    {
        if (content.Length >= 3
            && content[0] == 0xFF
            && content[1] == 0xD8
            && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.Length >= 8
            && content[0] == 0x89
            && content[1] == 0x50
            && content[2] == 0x4E
            && content[3] == 0x47
            && content[4] == 0x0D
            && content[5] == 0x0A
            && content[6] == 0x1A
            && content[7] == 0x0A)
        {
            return "image/png";
        }

        if (content.Length >= 12
            && content[0] == 0x52
            && content[1] == 0x49
            && content[2] == 0x46
            && content[3] == 0x46
            && content[8] == 0x57
            && content[9] == 0x45
            && content[10] == 0x42
            && content[11] == 0x50)
        {
            return "image/webp";
        }

        return null;
    }

    private static string GetExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "bin"
        };
    }
}
