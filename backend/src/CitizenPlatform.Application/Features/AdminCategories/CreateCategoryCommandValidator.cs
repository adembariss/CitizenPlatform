using FluentValidation;

namespace CitizenPlatform.Application.Features.AdminCategories;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50);
    }
}
