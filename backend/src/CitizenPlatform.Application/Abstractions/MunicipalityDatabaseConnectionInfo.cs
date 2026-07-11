using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public sealed record MunicipalityDatabaseConnectionInfo(
    Guid MunicipalityId,
    MunicipalityDbProvider Provider,
    string ConnectionName,
    string ConnectionString);
