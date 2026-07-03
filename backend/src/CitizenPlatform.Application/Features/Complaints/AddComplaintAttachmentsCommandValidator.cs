using FluentValidation;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed class AddComplaintAttachmentsCommandValidator : AbstractValidator<AddComplaintAttachmentsCommand>
{
    public AddComplaintAttachmentsCommandValidator()
    {
        RuleFor(command => command.TrackingCode)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(command => command.Attachments)
            .NotEmpty()
            .WithMessage("At least one attachment file is required.");
    }
}
