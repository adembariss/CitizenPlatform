using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Abstractions;

public interface IGeoMunicipalityResolver
{
    Task<MunicipalityResolveResult> ResolveByCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken ct);
}
