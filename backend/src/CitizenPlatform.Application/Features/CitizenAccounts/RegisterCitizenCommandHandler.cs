using System.Security.Cryptography;
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
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    private readonly IValidator<RegisterCitizenCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly ICitizenRepository _citizenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISmsSender _smsSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCitizenCommandHandler(
        IValidator<RegisterCitizenCommand> validator,
        IUserRepository userRepository,
        ICitizenRepository citizenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ISmsSender smsSender,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _userRepository = userRepository;
        _citizenRepository = citizenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _smsSender = smsSender;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    internal static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public async Task<Result<CitizenAuthResultDto>> HandleAsync(RegisterCitizenCommand command, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<CitizenAuthResultDto>.Failure(
                "Validation failed.",
                validation.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var phone = TurkishPhoneNumber.Normalize(command.PhoneNumber)!;

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return Result<CitizenAuthResultDto>.Failure("Bu e-posta ile bir hesap zaten var.");
        }

        var displayName = string.IsNullOrWhiteSpace(command.FullName)
            ? DeriveNameFromEmail(email)
            : command.FullName.Trim();

        var code = GenerateCode();

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var user = User.Create(email, displayName, UserType.Citizen);
            user.SetPasswordHash(_passwordHasher.Hash(command.Password));
            await _userRepository.AddAsync(user, ct);

            var citizen = Citizen.Create(user.Id, displayName, phone, email);
            citizen.StartPhoneVerification(code, _dateTimeProvider.UtcNow.Add(CodeLifetime));
            await _citizenRepository.AddAsync(citizen, ct);

            await _smsSender.SendAsync(phone, $"Belediyem doğrulama kodunuz: {code}", ct);

            var token = _tokenService.CreateAccessToken(new AccessTokenRequest(
                user.Id, user.Email, user.DisplayName, user.UserType, MunicipalityId: null, Roles: Array.Empty<string>()));

            var refreshToken = await _refreshTokenService.IssueAsync(user.Id, ct);

            var currentUser = new CurrentUserDto(
                user.Id, user.DisplayName, user.Email, user.UserType.ToString(),
                MunicipalityId: null, MunicipalityName: null, Roles: Array.Empty<string>());

            var response = new CitizenAuthResultDto(
                token.AccessToken,
                token.ExpiresAt,
                currentUser,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                PhoneVerified: false,
                VerificationCodePreview: _smsSender.RevealsCode ? code : null);

            return Result<CitizenAuthResultDto>.Success(response);
        }, cancellationToken);
    }

    private static string DeriveNameFromEmail(string email)
    {
        var localPart = email.Split('@')[0];
        return string.IsNullOrWhiteSpace(localPart) ? "Vatandaş" : localPart;
    }
}
