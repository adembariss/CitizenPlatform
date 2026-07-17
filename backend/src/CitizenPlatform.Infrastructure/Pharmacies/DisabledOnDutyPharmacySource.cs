using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Infrastructure.Pharmacies;

/// <summary>Canlı sağlayıcı yapılandırılmadığında kullanılan boş kaynak; sorgular DB'ye düşer.</summary>
public sealed class DisabledOnDutyPharmacySource : ILiveOnDutyPharmacySource
{
    public bool IsEnabled => false;

    public Task<IReadOnlyList<PharmacyDto>> GetOnDutyAsync(string? province, string? district, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PharmacyDto>>(Array.Empty<PharmacyDto>());

    public Task<IReadOnlyList<PharmacyDto>> GetNearbyOnDutyAsync(double latitude, double longitude, int limit, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PharmacyDto>>(Array.Empty<PharmacyDto>());
}
