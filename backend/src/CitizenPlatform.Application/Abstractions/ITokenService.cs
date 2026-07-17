using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public sealed record AccessTokenRequest(
    Guid UserId,
    string Email,
    string DisplayName,
    UserType UserType,
    Guid? MunicipalityId,
    IReadOnlyCollection<string> Roles,
    Guid? InstitutionId = null);

public sealed record AccessTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AccessTokenRequest request);
}
