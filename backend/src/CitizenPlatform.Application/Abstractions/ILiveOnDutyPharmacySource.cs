using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Abstractions;

/// <summary>
/// Canlı nöbetçi eczane veri kaynağı (ör. NosyAPI). Yapılandırılmadıysa <see cref="IsEnabled"/>
/// false döner ve sorgular veritabanı örnek verisine düşer.
/// </summary>
public interface ILiveOnDutyPharmacySource
{
    bool IsEnabled { get; }

    /// <summary>İl/ilçe (opsiyonel) nöbetçi eczaneler. Her ikisi de boşsa tüm Türkiye.</summary>
    Task<IReadOnlyList<PharmacyDto>> GetOnDutyAsync(string? province, string? district, CancellationToken cancellationToken);

    /// <summary>Konuma en yakın nöbetçi eczaneler (mesafe km ile).</summary>
    Task<IReadOnlyList<PharmacyDto>> GetNearbyOnDutyAsync(double latitude, double longitude, int limit, CancellationToken cancellationToken);
}
