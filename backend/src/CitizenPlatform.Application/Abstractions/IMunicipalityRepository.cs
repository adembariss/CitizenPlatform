using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public sealed record DistrictRow(Guid Id, string Name, string Code, double? Latitude, double? Longitude);

public interface IMunicipalityRepository
{
    Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetProvincesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DistrictRow>> GetDistrictsByProvinceAsync(string province, CancellationToken cancellationToken);
}
