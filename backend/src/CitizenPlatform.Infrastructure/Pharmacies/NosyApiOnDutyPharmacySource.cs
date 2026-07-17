using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CitizenPlatform.Infrastructure.Pharmacies;

/// <summary>
/// NosyAPI (https://www.nosyapi.com) nöbetçi eczane servisinden canlı veri çeker.
/// API anahtarı yalnızca sunucuda tutulur (X-NSYP header). Kredi tüketimini azaltmak için
/// sonuçlar bellekte cache'lenir: tüm Türkiye 6 saat, il/ilçe 1 saat, en yakın 15 dk.
/// </summary>
public sealed class NosyApiOnDutyPharmacySource : ILiveOnDutyPharmacySource
{
    private static readonly TimeSpan AllTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan CityTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan NearbyTtl = TimeSpan.FromMinutes(15);

    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _http;
    private readonly ILogger<NosyApiOnDutyPharmacySource> _logger;

    public NosyApiOnDutyPharmacySource(HttpClient http, ILogger<NosyApiOnDutyPharmacySource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public bool IsEnabled => true;

    public Task<IReadOnlyList<PharmacyDto>> GetOnDutyAsync(string? province, string? district, CancellationToken cancellationToken)
    {
        var citySlug = Slugify(province);
        var districtSlug = Slugify(district);

        // İl verilmediyse tüm Türkiye (81 kredi) — uzun cache. İl varsa şehir sorgusu (1 kredi).
        if (string.IsNullOrEmpty(citySlug))
        {
            return GetOrFetchAsync("onduty:all", AllTtl, "pharmacies-on-duty/all", cancellationToken);
        }

        var path = string.IsNullOrEmpty(districtSlug)
            ? $"pharmacies-on-duty?city={Uri.EscapeDataString(citySlug)}"
            : $"pharmacies-on-duty?city={Uri.EscapeDataString(citySlug)}&district={Uri.EscapeDataString(districtSlug)}";

        return GetOrFetchAsync($"onduty:{citySlug}:{districtSlug}", CityTtl, path, cancellationToken);
    }

    public async Task<IReadOnlyList<PharmacyDto>> GetNearbyOnDutyAsync(
        double latitude,
        double longitude,
        int limit,
        CancellationToken cancellationToken)
    {
        // 2 ondalık ≈ ~1 km ızgara: yakın konumlar aynı cache'i paylaşır.
        var key = $"nearby:{latitude:F2}:{longitude:F2}";
        var path = $"pharmacies-on-duty/locations?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                   + $"&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        var list = await GetOrFetchAsync(key, NearbyTtl, path, cancellationToken);

        return list
            .Select(p => p with { DistanceKm = Math.Round(HaversineKm(latitude, longitude, p.Latitude, p.Longitude), 2) })
            .OrderBy(p => p.DistanceKm)
            .Take(limit)
            .ToArray();
    }

    private async Task<IReadOnlyList<PharmacyDto>> GetOrFetchAsync(
        string cacheKey,
        TimeSpan ttl,
        string relativePath,
        CancellationToken cancellationToken)
    {
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Data;
        }

        try
        {
            using var response = await _http.GetAsync(relativePath, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<NosyResponse>(stream, JsonOptions, cancellationToken);

            var data = (payload?.Data ?? new List<NosyPharmacy>())
                .Where(item => item is not null && item.Latitude != 0 && item.Longitude != 0)
                .Select(Map)
                .ToArray();

            Cache[cacheKey] = new CacheEntry(DateTimeOffset.UtcNow.Add(ttl), data);
            return data;
        }
        catch (Exception ex)
        {
            // Hata durumunda (ağ/limit) varsa bayat cache'i döndür, yoksa boş — DB'ye düşmez.
            _logger.LogWarning(ex, "NosyAPI nöbetçi eczane sorgusu başarısız: {Path}", relativePath);
            return cached.Data ?? Array.Empty<PharmacyDto>();
        }
    }

    private static PharmacyDto Map(NosyPharmacy p)
    {
        var name = string.IsNullOrWhiteSpace(p.PharmacyName) ? "Eczane" : p.PharmacyName!.Trim();
        return new PharmacyDto(
            DeterministicId($"{name}|{p.Latitude}|{p.Longitude}"),
            name,
            (p.City ?? string.Empty).Trim(),
            (p.District ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(p.Address) ? null : p.Address!.Trim(),
            string.IsNullOrWhiteSpace(p.Phone) ? null : p.Phone!.Trim(),
            p.Latitude,
            p.Longitude,
            true,
            null);
    }

    private static Guid DeterministicId(string seed)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;
        static double ToRad(double d) => d * Math.PI / 180.0;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return earthRadiusKm * 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    // NosyAPI şehir/ilçe için slug bekler (istanbul, kadikoy). Türkçe karakterleri sadeleştir.
    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.EndsWith(" Belediyesi", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^" Belediyesi".Length];
        }

        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            switch (ch)
            {
                case 'ç' or 'Ç': sb.Append('c'); break;
                case 'ğ' or 'Ğ': sb.Append('g'); break;
                case 'ı' or 'I': sb.Append('i'); break;
                case 'İ' or 'i': sb.Append('i'); break;
                case 'ö' or 'Ö': sb.Append('o'); break;
                case 'ş' or 'Ş': sb.Append('s'); break;
                case 'ü' or 'Ü': sb.Append('u'); break;
                case ' ' or '_' or '/': sb.Append('-'); break;
                default:
                    if (char.IsLetterOrDigit(ch))
                    {
                        sb.Append(char.ToLowerInvariant(ch));
                    }
                    break;
            }
        }

        var slug = sb.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }

    private sealed record CacheEntry(DateTimeOffset ExpiresAt, IReadOnlyList<PharmacyDto> Data);

    private sealed record NosyResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("data")] List<NosyPharmacy>? Data);

    private sealed record NosyPharmacy(
        [property: JsonPropertyName("pharmacyName")] string? PharmacyName,
        [property: JsonPropertyName("address")] string? Address,
        [property: JsonPropertyName("city")] string? City,
        [property: JsonPropertyName("district")] string? District,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude);
}
