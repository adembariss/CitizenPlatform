namespace CitizenPlatform.Application.Abstractions;

public interface IFileSafetyScanner
{
    Task<FileSafetyScanResult> ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
