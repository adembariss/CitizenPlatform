using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public interface IComplaintRepository
{
    Task AddAsync(Complaint complaint, CancellationToken cancellationToken);

    Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken);
}
