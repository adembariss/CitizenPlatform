using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class PharmacyRepository : IPharmacyRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public PharmacyRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Pharmacy>> ListAsync(
        string? province,
        string? district,
        bool onDutyOnly,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Pharmacies.AsNoTracking().AsQueryable();

        if (onDutyOnly)
        {
            query = query.Where(pharmacy => pharmacy.IsOnDuty);
        }

        if (!string.IsNullOrWhiteSpace(province))
        {
            query = query.Where(pharmacy => pharmacy.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(district))
        {
            query = query.Where(pharmacy => pharmacy.District == district);
        }

        return await query
            .OrderByDescending(pharmacy => pharmacy.IsOnDuty)
            .ThenBy(pharmacy => pharmacy.District)
            .ThenBy(pharmacy => pharmacy.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Pharmacy>> NearbyAsync(
        double latitude,
        double longitude,
        bool onDutyOnly,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Pharmacies.AsNoTracking().AsQueryable();

        if (onDutyOnly)
        {
            query = query.Where(pharmacy => pharmacy.IsOnDuty);
        }

        // Squared-degree distance is enough to rank nearby points; exact km is computed later.
        return await query
            .OrderBy(pharmacy =>
                (pharmacy.Latitude - latitude) * (pharmacy.Latitude - latitude) +
                (pharmacy.Longitude - longitude) * (pharmacy.Longitude - longitude))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
