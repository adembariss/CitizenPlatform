using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CitizenPlatform.Infrastructure.Persistence;

public sealed class MunicipalityDatabaseConnectionResolver : IMunicipalityDatabaseConnectionResolver
{
    private readonly CitizenPlatformDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public MunicipalityDatabaseConnectionResolver(
        CitizenPlatformDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<Result<MunicipalityDatabaseConnectionInfo>> ResolveAsync(
        Guid municipalityId,
        CancellationToken cancellationToken)
    {
        var connection = await _dbContext.MunicipalityDatabaseConnections
            .AsNoTracking()
            .Where(candidate => candidate.MunicipalityId == municipalityId && candidate.IsActive)
            .OrderBy(candidate => candidate.ConnectionName)
            .FirstOrDefaultAsync(cancellationToken);

        if (connection is not null)
        {
            return Result<MunicipalityDatabaseConnectionInfo>.Success(
                new MunicipalityDatabaseConnectionInfo(
                    municipalityId,
                    connection.Provider,
                    connection.ConnectionName,
                    connection.EncryptedConnectionString));
        }

        var fallbackConnectionString = _configuration["SAMPLE_MUNICIPALITY_DB_CONNECTION"]
            ?? _configuration["MUNICIPALITY_DB_CONNECTION_STRING"]
            ?? _configuration["MunicipalityDb:ConnectionString"];

        return string.IsNullOrWhiteSpace(fallbackConnectionString)
            ? Result<MunicipalityDatabaseConnectionInfo>.Failure("No active municipality database connection was found.")
            : Result<MunicipalityDatabaseConnectionInfo>.Success(
                new MunicipalityDatabaseConnectionInfo(
                    municipalityId,
                    MunicipalityDbProvider.PostgreSql,
                    "sample-municipality-db",
                    fallbackConnectionString));
    }
}
