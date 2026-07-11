using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public interface IMunicipalityComplaintWriter
{
    MunicipalityDbProvider Provider { get; }

    Task<Result<MunicipalityComplaintWriteResult>> WriteComplaintCreatedAsync(
        MunicipalityDatabaseConnectionInfo connectionInfo,
        MunicipalityComplaintCreatedPayload payload,
        CancellationToken cancellationToken);
}
