using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.Pharmacies;

internal static class GeoDistance
{
    // Haversine distance in kilometres.
    public static double Km(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return earthRadiusKm * 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}

public sealed class PharmacyListQueryHandler
{
    public const int DefaultLimit = 500;
    public const int MaxLimit = 1000;

    private readonly IPharmacyRepository _repository;

    public PharmacyListQueryHandler(IPharmacyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PharmacyDto>> HandleAsync(
        string? province,
        string? district,
        bool onDutyOnly,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = limit is null or <= 0 ? DefaultLimit : Math.Min(limit.Value, MaxLimit);
        var rows = await _repository.ListAsync(province, district, onDutyOnly, take, cancellationToken);
        return rows.Select(pharmacy => Map(pharmacy, null)).ToArray();
    }

    internal static PharmacyDto Map(Pharmacy pharmacy, double? distanceKm) => new(
        pharmacy.Id,
        pharmacy.Name,
        pharmacy.Province,
        pharmacy.District,
        pharmacy.AddressText,
        pharmacy.PhoneNumber,
        pharmacy.Latitude,
        pharmacy.Longitude,
        pharmacy.IsOnDuty,
        distanceKm);
}

public sealed class NearbyPharmacyQueryHandler
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    private readonly IPharmacyRepository _repository;

    public NearbyPharmacyQueryHandler(IPharmacyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PharmacyDto>> HandleAsync(
        double latitude,
        double longitude,
        bool onDutyOnly,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = limit is null or <= 0 ? DefaultLimit : Math.Min(limit.Value, MaxLimit);
        var rows = await _repository.NearbyAsync(latitude, longitude, onDutyOnly, take, cancellationToken);

        return rows
            .Select(pharmacy => PharmacyListQueryHandler.Map(
                pharmacy,
                Math.Round(GeoDistance.Km(latitude, longitude, pharmacy.Latitude, pharmacy.Longitude), 2)))
            .ToArray();
    }
}
