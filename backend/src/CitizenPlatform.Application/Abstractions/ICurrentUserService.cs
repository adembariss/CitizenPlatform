using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    Guid? MunicipalityId { get; }

    UserType? UserType { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsSystemAdmin { get; }

    Guid? UserGuid { get; }
}
