using FluentValidation;

namespace CitizenPlatform.Application.Features.AdminDepartments;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50);
    }
}
