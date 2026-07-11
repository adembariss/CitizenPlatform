using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.AdminCategories;

public sealed class UpdateCategoryCommandHandler
{
    private readonly IComplaintCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(IComplaintCategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminScopedResult<CategoryDto>> HandleAsync(
        UpdateCategoryCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
        {
            return AdminScopedResult<CategoryDto>.AsNotFound();
        }

        var canManage = category.MunicipalityId is null
            ? scope.IsSystemAdmin
            : scope.CanAccess(category.MunicipalityId.Value);

        if (!canManage)
        {
            return AdminScopedResult<CategoryDto>.AsNotFound();
        }

        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            category.Rename(command.Name);
        }

        if (command.IsActive is true)
        {
            category.Activate();
        }
        else if (command.IsActive is false)
        {
            category.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AdminScopedResult<CategoryDto>.Success(
            new CategoryDto(category.Id, category.MunicipalityId, category.Name, category.Code, category.IsActive));
    }
}
