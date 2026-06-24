namespace CitizenPlatform.Integrations.MunicipalityDb;

public sealed class MunicipalityDbOptions
{
    public const string SectionName = "MunicipalityDb";

    public string ConnectionString { get; init; } = string.Empty;
}
