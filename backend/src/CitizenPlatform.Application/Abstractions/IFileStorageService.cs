namespace CitizenPlatform.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
}
