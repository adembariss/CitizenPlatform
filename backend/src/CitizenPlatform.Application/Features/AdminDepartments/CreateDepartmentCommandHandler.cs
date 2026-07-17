using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;
using FluentValidation;

namespace CitizenPlatform.Application.Features.AdminDepartments;

public sealed class CreateDepartmentCommandHandler
{
    private readonly IValidator<CreateDepartmentCommand> _validator;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDepartmentCommandHandler(
        IValidator<CreateDepartmentCommand> validator,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentDto>> HandleAsync(
        CreateDepartmentCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<DepartmentDto>.Failure(
                "Validation failed.",
                validationResult.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        var municipalityId = scope.IsSystemAdmin ? command.MunicipalityId : scope.MunicipalityId;
        if (municipalityId is null)
        {
            return Result<DepartmentDto>.Failure("municipalityId is required.");
        }

        if (await _departmentRepository.CodeExistsAsync(municipalityId.Value, command.Code, cancellationToken))
        {
            return Result<DepartmentDto>.Failure("A department with this code already exists.");
        }

        var department = Department.Create(municipalityId.Value, command.Name, command.Code);
        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DepartmentDto>.Success(
            new DepartmentDto(department.Id, department.MunicipalityId, department.Name, department.Code, department.IsActive, department.InstitutionId));
    }
}
