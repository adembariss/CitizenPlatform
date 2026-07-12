using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed class CitizenComplaintListQueryHandler
{
    private readonly ICitizenRepository _citizenRepository;
    private readonly ICitizenComplaintRepository _citizenComplaintRepository;

    public CitizenComplaintListQueryHandler(
        ICitizenRepository citizenRepository,
        ICitizenComplaintRepository citizenComplaintRepository)
    {
        _citizenRepository = citizenRepository;
        _citizenComplaintRepository = citizenComplaintRepository;
    }

    public async Task<IReadOnlyList<CitizenComplaintListItemDto>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var citizen = await _citizenRepository.GetByUserIdAsync(userId, cancellationToken);
        if (citizen is null)
        {
            return Array.Empty<CitizenComplaintListItemDto>();
        }

        return await _citizenComplaintRepository.ListByCitizenAsync(citizen.Id, cancellationToken);
    }
}
