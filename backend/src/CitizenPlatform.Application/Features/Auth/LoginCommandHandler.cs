using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using FluentValidation;

namespace CitizenPlatform.Application.Features.Auth;

public sealed class LoginCommandHandler
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IValidator<LoginCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IValidator<LoginCommand> validator,
        IUserRepository userRepository,
        IMunicipalityRepository municipalityRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService)
    {
        _validator = validator;
        _userRepository = userRepository;
        _municipalityRepository = municipalityRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<LoginResponseDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<LoginResponseDto>.Failure(
                "Validation failed.",
                validationResult.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Result<LoginResponseDto>.Failure(InvalidCredentialsMessage);
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result<LoginResponseDto>.Failure(InvalidCredentialsMessage);
        }

        var currentUser = await CurrentUserComposer.ComposeAsync(
            user,
            _userRepository,
            _municipalityRepository,
            cancellationToken);

        var token = _tokenService.CreateAccessToken(new AccessTokenRequest(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserType,
            currentUser.MunicipalityId,
            currentUser.Roles));

        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            token.AccessToken,
            token.ExpiresAt,
            currentUser,
            refreshToken.Token,
            refreshToken.ExpiresAt));
    }
}
