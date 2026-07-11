using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly CitizenPlatformDbContext _dbContext;

    public DepartmentRepository(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Departments.FirstOrDefaultAsync(
            department => department.Id == id,
            cancellationToken);
    }
}
