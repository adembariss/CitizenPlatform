using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

public sealed class MunicipalityDatabaseConnection : AuditableEntity
{
    private MunicipalityDatabaseConnection()
    {
    }

    private MunicipalityDatabaseConnection(
        Guid id,
        Guid municipalityId,
        MunicipalityDbProvider provider,
        string connectionName,
        string encryptedConnectionString)
        : base(id)
    {
        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        Provider = provider;
        ConnectionName = Guard.AgainstEmpty(connectionName, nameof(connectionName), 100);
        EncryptedConnectionString = Guard.AgainstEmpty(encryptedConnectionString, nameof(encryptedConnectionString), 4000);
        IsActive = true;
    }

    public Guid MunicipalityId { get; private set; }

    public MunicipalityDbProvider Provider { get; private set; }

    public string ConnectionName { get; private set; } = string.Empty;

    public string EncryptedConnectionString { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static MunicipalityDatabaseConnection Create(
        Guid municipalityId,
        MunicipalityDbProvider provider,
        string connectionName,
        string encryptedConnectionString)
    {
        return new MunicipalityDatabaseConnection(Guid.NewGuid(), municipalityId, provider, connectionName, encryptedConnectionString);
    }
}
