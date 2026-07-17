using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.AdminDepartments;

public sealed class AdminDepartmentListQueryHandler
{
    private readonly IDepartmentRepository _departmentRepository;

    public AdminDepartmentListQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IReadOnlyList<DepartmentDto>> HandleAsync(
        Guid? requestedMunicipalityId,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var municipalityId = scope.ResolveListFilter(requestedMunicipalityId);
        var departments = await _departmentRepository.ListAsync(municipalityId, scope.InstitutionId, cancellationToken);

        return departments
            .Select(department => new DepartmentDto(
                department.Id, department.MunicipalityId, department.Name, department.Code, department.IsActive, department.InstitutionId))
            .ToArray();
    }
}
