namespace CitizenPlatform.Application.DTOs;

public sealed record MunicipalityResolveResult(
    bool IsSuccess,
    Guid? MunicipalityId,
    string? MunicipalityName,
    string? MunicipalityCode,
    string? FailureReason)
{
    public static MunicipalityResolveResult Success(Guid municipalityId, string municipalityName, string municipalityCode)
    {
        return new MunicipalityResolveResult(true, municipalityId, municipalityName, municipalityCode, null);
    }

    public static MunicipalityResolveResult Failure(string failureReason)
    {
        return new MunicipalityResolveResult(false, null, null, null, failureReason);
    }
}
