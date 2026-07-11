using CitizenPlatform.Application.Common.Models;

namespace CitizenPlatform.Application.Abstractions;

public interface IMunicipalityDatabaseConnectionResolver
{
    Task<Result<MunicipalityDatabaseConnectionInfo>> ResolveAsync(
        Guid municipalityId,
        CancellationToken cancellationToken);
}
