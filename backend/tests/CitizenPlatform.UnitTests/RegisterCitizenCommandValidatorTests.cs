using CitizenPlatform.Application.Features.CitizenAccounts;
using FluentValidation.TestHelper;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class RegisterCitizenCommandValidatorTests
{
    private readonly RegisterCitizenCommandValidator _validator = new();

    [Fact]
    public void Valid_WhenEmailPhoneAndPasswordAreCorrect()
    {
        var command = new RegisterCitizenCommand("ada@example.com", "0532 123 45 67", "Sifre123", "Ada Lovelace");

        var result = _validator.TestValidate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Invalid_WhenPhoneIsMissing()
    {
        var command = new RegisterCitizenCommand("ada@example.com", "", "Sifre123", null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.PhoneNumber);
    }

    [Fact]
    public void Invalid_WhenPhoneIsNotTurkishMobile()
    {
        var command = new RegisterCitizenCommand("ada@example.com", "0212 123 45 67", "Sifre123", null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.PhoneNumber);
    }

    [Fact]
    public void Invalid_WhenEmailIsMalformed()
    {
        var command = new RegisterCitizenCommand("not-an-email", "05321234567", "Sifre123", null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("short1")]        // 8 karakterden kısa
    [InlineData("onlyletters")]   // rakam yok
    [InlineData("12345678")]      // harf yok
    public void Invalid_WhenPasswordIsWeak(string password)
    {
        var command = new RegisterCitizenCommand("ada@example.com", "05321234567", password, null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Password);
    }
}
