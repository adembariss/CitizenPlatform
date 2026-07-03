using CitizenPlatform.Application.Common.Models;

namespace CitizenPlatform.Application.Abstractions;

public interface IFileStorageService
{
    Task<Result<FileStorageSaveResult>> SaveAsync(
        FileStorageSaveRequest request,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
