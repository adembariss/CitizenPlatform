using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;
using FluentValidation;

namespace CitizenPlatform.Application.Features.AdminCategories;

public sealed class CreateCategoryCommandHandler
{
    private readonly IValidator<CreateCategoryCommand> _validator;
    private readonly IComplaintCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(
        IValidator<CreateCategoryCommand> validator,
        IComplaintCategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> HandleAsync(
        CreateCategoryCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<CategoryDto>.Failure(
                "Validation failed.",
                validationResult.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        var municipalityId = scope.IsSystemAdmin ? command.MunicipalityId : scope.MunicipalityId;

        if (await _categoryRepository.CodeExistsAsync(municipalityId, command.Code, cancellationToken))
        {
            return Result<CategoryDto>.Failure("A category with this code already exists.");
        }

        var category = ComplaintCategory.Create(command.Name, command.Code, municipalityId);
        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Success(
            new CategoryDto(category.Id, category.MunicipalityId, category.Name, category.Code, category.IsActive));
    }
}
