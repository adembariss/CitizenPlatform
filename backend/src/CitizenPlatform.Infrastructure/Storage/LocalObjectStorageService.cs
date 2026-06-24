using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Infrastructure.Storage;

public sealed class LocalObjectStorageService : IFileStorageService
{
    public Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var objectKey = $"{Guid.NewGuid():N}";
        return Task.FromResult(objectKey);
    }

    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        return Task.FromResult<Stream>(Stream.Null);
    }
}
