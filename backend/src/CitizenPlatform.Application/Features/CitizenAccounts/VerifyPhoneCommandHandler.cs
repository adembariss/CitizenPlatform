using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;

namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed record VerifyPhoneCommand(Guid UserId, string Code);

public sealed record ResendCodeCommand(Guid UserId);

public sealed class VerifyPhoneCommandHandler
{
    private readonly ICitizenRepository _citizenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyPhoneCommandHandler(
        ICitizenRepository citizenRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _citizenRepository = citizenRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> HandleAsync(VerifyPhoneCommand command, CancellationToken cancellationToken)
    {
        var citizen = await _citizenRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (citizen is null)
        {
            return Result<bool>.Failure("Vatandaş profili bulunamadı.");
        }

        if (!citizen.VerifyPhone(command.Code, _dateTimeProvider.UtcNow))
        {
            return Result<bool>.Failure("Kod hatalı veya süresi dolmuş.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
