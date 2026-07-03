using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class LocalFileStorageServiceTests
{
    private static readonly byte[] MinimalJpeg = [0xFF, 0xD8, 0xFF, 0xD9];

    [Fact]
    public async Task SaveAsync_WhenJpegIsValid_SavesFile()
    {
        var rootPath = CreateTempRootPath();
        try
        {
            var service = CreateService(rootPath);
            await using var content = new MemoryStream(MinimalJpeg);

            var result = await service.SaveAsync(
                new FileStorageSaveRequest(content, "photo.jpg", "image/jpeg", MinimalJpeg.Length),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("photo.jpg", result.Value!.OriginalFileName);
            Assert.NotEqual("photo.jpg", result.Value.FileName);
            Assert.Equal("image/jpeg", result.Value.ContentType);
            Assert.Equal(64, result.Value.Sha256Hash.Length);

            await using var stored = await service.OpenReadAsync(result.Value.ObjectKey, CancellationToken.None);
            Assert.True(stored.Length > 0);
        }
        finally
        {
            DeleteTempRootPath(rootPath);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenMimeTypeIsInvalid_ReturnsFailure()
    {
        var rootPath = CreateTempRootPath();
        try
        {
            var service = CreateService(rootPath);
            await using var content = new MemoryStream(MinimalJpeg);

            var result = await service.SaveAsync(
                new FileStorageSaveRequest(content, "photo.html", "text/html", MinimalJpeg.Length),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("content type", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTempRootPath(rootPath);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenFileExceedsLimit_ReturnsFailure()
    {
        var rootPath = CreateTempRootPath();
        try
        {
            var service = CreateService(rootPath, maxFileSizeBytes: 3);
            await using var content = new MemoryStream(MinimalJpeg);

            var result = await service.SaveAsync(
                new FileStorageSaveRequest(content, "photo.jpg", "image/jpeg", MinimalJpeg.Length),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("size", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTempRootPath(rootPath);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenJpegHasNoExif_ReturnsEmptyMetadata()
    {
        var reader = new ExifImageMetadataReader();
        await using var content = new MemoryStream(MinimalJpeg);

        var metadata = await reader.ReadAsync(content, "image/jpeg", CancellationToken.None);

        Assert.Null(metadata.GpsLocation);
        Assert.Null(metadata.TakenAt);
    }

    private static LocalFileStorageService CreateService(string rootPath, long maxFileSizeBytes = 10 * 1024 * 1024)
    {
        return new LocalFileStorageService(
            Options.Create(new ObjectStorageOptions
            {
                LocalRootPath = rootPath,
                MaxFileSizeBytes = maxFileSizeBytes
            }),
            new NoOpFileSafetyScanner());
    }

    private static string CreateTempRootPath()
    {
        return Path.Combine(Path.GetTempPath(), "CitizenPlatformTests", Guid.NewGuid().ToString("N"));
    }

    private static void DeleteTempRootPath(string rootPath)
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }
}
