using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Abstractions;

public sealed record UserRoleAssignment(string RoleKey, string RoleName, Guid? MunicipalityId);

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserRoleAssignment>> GetActiveRoleAssignmentsAsync(Guid userId, CancellationToken cancellationToken);
}
