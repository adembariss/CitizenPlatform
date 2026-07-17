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

    public async Task<IReadOnlyList<Department>> ListAsync(
        Guid? municipalityId,
        Guid? institutionId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Departments.AsQueryable();

        if (institutionId is not null)
        {
            // Kurum yöneticisi: yalnızca kendi kurumunun birimleri.
            query = query.Where(department => department.InstitutionId == institutionId);
        }
        else
        {
            // Belediye/SystemAdmin: kurum birimleri hariç. Belediye verilmediyse (SystemAdmin)
            // tüm belediyelerin birimleri döner; verildiyse yalnızca o belediyeninkiler.
            query = query.Where(department => department.InstitutionId == null);

            if (municipalityId is not null)
            {
                query = query.Where(department => department.MunicipalityId == municipalityId);
            }
        }

        return await query.OrderBy(department => department.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(Guid municipalityId, string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Departments.AnyAsync(
            department => department.MunicipalityId == municipalityId && department.Code == code,
            cancellationToken);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken)
    {
        await _dbContext.Departments.AddAsync(department, cancellationToken);
    }
}
