using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using FluentValidation;

namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed class RegisterCitizenCommandHandler
{
    private readonly IValidator<RegisterCitizenCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly ICitizenRepository _citizenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCitizenCommandHandler(
        IValidator<RegisterCitizenCommand> validator,
        IUserRepository userRepository,
        ICitizenRepository citizenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _userRepository = userRepository;
        _citizenRepository = citizenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponseDto>> HandleAsync(RegisterCitizenCommand command, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<LoginResponseDto>.Failure(
                "Validation failed.",
                validation.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var phone = TurkishPhoneNumber.Normalize(command.PhoneNumber)!;

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return Result<LoginResponseDto>.Failure("Bu e-posta ile bir hesap zaten var.");
        }

        var displayName = string.IsNullOrWhiteSpace(command.FullName)
            ? DeriveNameFromEmail(email)
            : command.FullName.Trim();

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var user = User.Create(email, displayName, UserType.Citizen);
            user.SetPasswordHash(_passwordHasher.Hash(command.Password));
            await _userRepository.AddAsync(user, ct);

            var citizen = Citizen.Create(user.Id, displayName, phone, email);
            await _citizenRepository.AddAsync(citizen, ct);

            var token = _tokenService.CreateAccessToken(new AccessTokenRequest(
                user.Id,
                user.Email,
                user.DisplayName,
                user.UserType,
                MunicipalityId: null,
                Roles: Array.Empty<string>()));

            var refreshToken = await _refreshTokenService.IssueAsync(user.Id, ct);

            var currentUser = new CurrentUserDto(
                user.Id,
                user.DisplayName,
                user.Email,
                user.UserType.ToString(),
                MunicipalityId: null,
                MunicipalityName: null,
                Roles: Array.Empty<string>());

            var response = new LoginResponseDto(
                token.AccessToken,
                token.ExpiresAt,
                currentUser,
                refreshToken.Token,
                refreshToken.ExpiresAt);

            return Result<LoginResponseDto>.Success(response);
        }, cancellationToken);
    }

    private static string DeriveNameFromEmail(string email)
    {
        var localPart = email.Split('@')[0];
        return string.IsNullOrWhiteSpace(localPart) ? "Vatandaş" : localPart;
    }
}
