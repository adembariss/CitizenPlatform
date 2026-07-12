using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Application.Features.Auth;

public sealed class LogoutCommandHandler
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return;
        }

        await _refreshTokenService.RevokeAsync(command.RefreshToken.Trim(), cancellationToken);
    }
}
