using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.Auth;

public sealed class GetCurrentUserQueryHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IInstitutionRepository _institutionRepository;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IMunicipalityRepository municipalityRepository,
        IInstitutionRepository institutionRepository)
    {
        _userRepository = userRepository;
        _municipalityRepository = municipalityRepository;
        _institutionRepository = institutionRepository;
    }

    public async Task<Result<CurrentUserDto>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<CurrentUserDto>.Failure("User not found.");
        }

        var currentUser = await CurrentUserComposer.ComposeAsync(
            user,
            _userRepository,
            _municipalityRepository,
            _institutionRepository,
            cancellationToken);

        return Result<CurrentUserDto>.Success(currentUser);
    }
}
