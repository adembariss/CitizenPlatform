using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.Auth;

public sealed class GetCurrentUserQueryHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IMunicipalityRepository _municipalityRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository, IMunicipalityRepository municipalityRepository)
    {
        _userRepository = userRepository;
        _municipalityRepository = municipalityRepository;
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
            cancellationToken);

        return Result<CurrentUserDto>.Success(currentUser);
    }
}
