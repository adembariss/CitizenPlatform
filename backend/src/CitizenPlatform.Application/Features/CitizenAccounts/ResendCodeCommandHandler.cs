using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;

namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed record ResendCodeResult(bool AlreadyVerified, string? VerificationCodePreview);

public sealed class ResendCodeCommandHandler
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    private readonly ICitizenRepository _citizenRepository;
    private readonly ISmsSender _smsSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ResendCodeCommandHandler(
        ICitizenRepository citizenRepository,
        ISmsSender smsSender,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _citizenRepository = citizenRepository;
        _smsSender = smsSender;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ResendCodeResult>> HandleAsync(ResendCodeCommand command, CancellationToken cancellationToken)
    {
        var citizen = await _citizenRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (citizen is null)
        {
            return Result<ResendCodeResult>.Failure("Vatandaş profili bulunamadı.");
        }

        if (citizen.PhoneVerified)
        {
            return Result<ResendCodeResult>.Success(new ResendCodeResult(AlreadyVerified: true, null));
        }

        var code = RegisterCitizenCommandHandler.GenerateCode();
        citizen.StartPhoneVerification(code, _dateTimeProvider.UtcNow.Add(CodeLifetime));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _smsSender.SendAsync(citizen.PhoneNumber ?? string.Empty, $"Belediyem doğrulama kodunuz: {code}", cancellationToken);

        return Result<ResendCodeResult>.Success(new ResendCodeResult(
            AlreadyVerified: false,
            VerificationCodePreview: _smsSender.RevealsCode ? code : null));
    }
}
