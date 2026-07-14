using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IPharmacyRepository
{
    Task<IReadOnlyList<Pharmacy>> ListAsync(
        string? province,
        string? district,
        bool onDutyOnly,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Pharmacy>> NearbyAsync(
        double latitude,
        double longitude,
        bool onDutyOnly,
        int limit,
        CancellationToken cancellationToken);
}
