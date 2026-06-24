using CitizenPlatform.Domain.Enums;
using FluentValidation;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed class CreateComplaintCommandValidator : AbstractValidator<CreateComplaintCommand>
{
    public CreateComplaintCommandValidator()
    {
        RuleFor(command => command.CategoryId)
            .NotEmpty();

        RuleFor(command => command.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(command => command.Title)
            .MaximumLength(200);

        RuleFor(command => command.CitizenFullName)
            .MaximumLength(200);

        RuleFor(command => command.CitizenPhoneNumber)
            .MaximumLength(40);

        RuleFor(command => command.CitizenEmail)
            .EmailAddress()
            .MaximumLength(320)
            .When(command => !string.IsNullOrWhiteSpace(command.CitizenEmail));

        RuleFor(command => command.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(command => command.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(command => command.AddressText)
            .MaximumLength(1000);

        RuleFor(command => command.Source)
            .Must(source => source is ComplaintSource.CitizenWeb or ComplaintSource.CitizenMobile)
            .WithMessage("Source must be CitizenWeb or CitizenMobile.");
    }
}
