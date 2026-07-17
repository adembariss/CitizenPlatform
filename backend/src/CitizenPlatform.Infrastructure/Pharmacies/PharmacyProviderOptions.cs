namespace CitizenPlatform.Infrastructure.Pharmacies;

public sealed class PharmacyProviderOptions
{
    public const string SectionName = "Pharmacies";

    /// <summary>"Database" (varsayılan, örnek veri) veya "NosyApi" (canlı nöbetçi eczane).</summary>
    public string Provider { get; init; } = "Database";

    public NosyApiOptions NosyApi { get; init; } = new();
}

public sealed class NosyApiOptions
{
    public string ApiBaseUrl { get; init; } = "https://www.nosyapi.com/apiv2/service";

    public string ApiKey { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
