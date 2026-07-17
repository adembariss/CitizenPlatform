using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.Auth;

public sealed class RefreshTokenCommandHandler
{
    private const string InvalidTokenMessage = "Invalid or expired refresh token.";

    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IInstitutionRepository _institutionRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IUserRepository userRepository,
        IMunicipalityRepository municipalityRepository,
        IInstitutionRepository institutionRepository,
        ITokenService tokenService)
    {
        _refreshTokenService = refreshTokenService;
        _userRepository = userRepository;
        _municipalityRepository = municipalityRepository;
        _institutionRepository = institutionRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponseDto>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result<LoginResponseDto>.Failure(InvalidTokenMessage);
        }

        var rotated = await _refreshTokenService.RotateAsync(command.RefreshToken.Trim(), cancellationToken);
        if (rotated is null)
        {
            return Result<LoginResponseDto>.Failure(InvalidTokenMessage);
        }

        var user = await _userRepository.GetByIdAsync(rotated.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            // The rotation already happened; make sure the replacement is unusable too.
            await _refreshTokenService.RevokeAsync(rotated.Token, cancellationToken);
            return Result<LoginResponseDto>.Failure(InvalidTokenMessage);
        }

        var currentUser = await CurrentUserComposer.ComposeAsync(
            user,
            _userRepository,
            _municipalityRepository,
            _institutionRepository,
            cancellationToken);

        var token = _tokenService.CreateAccessToken(new AccessTokenRequest(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserType,
            currentUser.MunicipalityId,
            currentUser.Roles,
            currentUser.InstitutionId));

        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            token.AccessToken,
            token.ExpiresAt,
            currentUser,
            rotated.Token,
            rotated.ExpiresAt));
    }
}
