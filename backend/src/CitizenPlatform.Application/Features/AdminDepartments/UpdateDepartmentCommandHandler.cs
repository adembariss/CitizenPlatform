using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.AdminDepartments;

public sealed class UpdateDepartmentCommandHandler
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDepartmentCommandHandler(IDepartmentRepository departmentRepository, IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminScopedResult<DepartmentDto>> HandleAsync(
        UpdateDepartmentCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(command.Id, cancellationToken);
        if (department is null || !CanManage(department, scope))
        {
            return AdminScopedResult<DepartmentDto>.AsNotFound();
        }

        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            department.Rename(command.Name);
        }

        if (command.IsActive is true)
        {
            department.Activate();
        }
        else if (command.IsActive is false)
        {
            department.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AdminScopedResult<DepartmentDto>.Success(
            new DepartmentDto(department.Id, department.MunicipalityId, department.Name, department.Code, department.IsActive, department.InstitutionId));
    }

    // Kurum birimini yalnızca o kurumun yöneticisi, belediye birimini yalnızca o belediye yönetir.
    private static bool CanManage(Department department, TenantScope scope)
    {
        if (department.InstitutionId is Guid institutionId)
        {
            return scope.InstitutionId == institutionId;
        }

        return department.MunicipalityId is Guid municipalityId && scope.CanAccess(municipalityId);
    }
}
