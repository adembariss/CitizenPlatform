using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed class CitizenProfileQueryHandler
{
    private readonly ICitizenRepository _citizenRepository;

    public CitizenProfileQueryHandler(ICitizenRepository citizenRepository)
    {
        _citizenRepository = citizenRepository;
    }

    public async Task<CitizenProfileDto?> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var citizen = await _citizenRepository.GetByUserIdAsync(userId, cancellationToken);
        if (citizen is null)
        {
            return null;
        }

        return new CitizenProfileDto(citizen.Id, citizen.FullName, citizen.Email ?? string.Empty, citizen.PhoneNumber);
    }
}
