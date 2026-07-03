namespace CitizenPlatform.Application.Abstractions;

public interface IImageMetadataReader
{
    Task<ImageMetadata> ReadAsync(Stream content, string contentType, CancellationToken cancellationToken);
}
