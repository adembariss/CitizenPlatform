using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Infrastructure.Storage;

public sealed class NoOpFileSafetyScanner : IFileSafetyScanner
{
    public Task<FileSafetyScanResult> ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(FileSafetyScanResult.Safe());
    }
}
