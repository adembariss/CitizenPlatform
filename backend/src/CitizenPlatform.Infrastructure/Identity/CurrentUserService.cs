using System.Security.Claims;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CitizenPlatform.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? UserId => IsAuthenticated
        ? Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;

    public Guid? UserGuid => Guid.TryParse(UserId, out var userId) ? userId : null;

    public Guid? MunicipalityId
    {
        get
        {
            var value = Principal?.FindFirstValue(JwtTokenService.MunicipalityIdClaimType);
            return Guid.TryParse(value, out var municipalityId) ? municipalityId : null;
        }
    }

    public UserType? UserType
    {
        get
        {
            var value = Principal?.FindFirstValue(JwtTokenService.UserTypeClaimType);
            return Enum.TryParse<UserType>(value, out var userType) ? userType : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? Array.Empty<string>();

    public bool IsSystemAdmin => UserType == Domain.Enums.UserType.SystemAdmin;
}
