using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    public string? UserId => null;

    public bool IsAuthenticated => false;
}
