using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Abstractions;

public interface ICitizenComplaintRepository
{
    Task<IReadOnlyList<CitizenComplaintListItemDto>> ListByCitizenAsync(Guid citizenId, CancellationToken cancellationToken);
}
