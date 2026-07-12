using CitizenPlatform.Application.Common;
using FluentValidation;

namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed class RegisterCitizenCommandValidator : AbstractValidator<RegisterCitizenCommand>
{
    public RegisterCitizenCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(320);

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur.")
            .Must(phone => TurkishPhoneNumber.IsValid(phone))
            .WithMessage("Geçerli bir Türkiye cep telefonu girin (örn. 0532 123 45 67).");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128)
            .Matches("[A-Za-zÇĞİÖŞÜçğıöşü]").WithMessage("Şifre en az bir harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");

        RuleFor(command => command.FullName)
            .MaximumLength(200);
    }
}
