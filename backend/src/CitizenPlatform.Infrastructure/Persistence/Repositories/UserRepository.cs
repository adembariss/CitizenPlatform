using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public UserRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleAssignment>> GetActiveRoleAssignmentsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
            from userRole in _dbContext.UserRoles
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && userRole.RevokedAt == null
            select new UserRoleAssignment(role.Key, role.Name, userRole.MunicipalityId, userRole.InstitutionId))
            .ToListAsync(cancellationToken);
    }
}
