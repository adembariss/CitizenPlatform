using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class ComplaintRepository : IComplaintRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public ComplaintRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Complaint complaint, CancellationToken cancellationToken)
    {
        await _dbContext.Complaints.AddAsync(complaint, cancellationToken);
    }

    public async Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Complaints.FirstOrDefaultAsync(complaint => complaint.Id == id, cancellationToken);
    }

    public async Task<Complaint?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
    {
        return await _dbContext.Complaints
            .Include(complaint => complaint.Attachments)
            .FirstOrDefaultAsync(complaint => complaint.TrackingCode == trackingCode, cancellationToken);
    }

    public async Task<bool> ExistsByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
    {
        return await _dbContext.Complaints.AnyAsync(complaint => complaint.TrackingCode == trackingCode, cancellationToken);
    }
}
