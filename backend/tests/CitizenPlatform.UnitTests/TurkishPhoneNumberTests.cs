using CitizenPlatform.Application.Common;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class TurkishPhoneNumberTests
{
    [Theory]
    [InlineData("05321234567", "+905321234567")]
    [InlineData("5321234567", "+905321234567")]
    [InlineData("+905321234567", "+905321234567")]
    [InlineData("0532 123 45 67", "+905321234567")]
    [InlineData("+90 532 123 45 67", "+905321234567")]
    [InlineData("(0532) 123-45-67", "+905321234567")]
    public void Normalize_AcceptsValidTurkishMobileFormats(string input, string expected)
    {
        Assert.Equal(expected, TurkishPhoneNumber.Normalize(input));
        Assert.True(TurkishPhoneNumber.IsValid(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")]
    [InlineData("02121234567")]      // sabit hat (5 ile başlamıyor)
    [InlineData("0532123456")]       // eksik hane
    [InlineData("053212345678")]     // fazla hane
    [InlineData("+15321234567")]     // yabancı ülke kodu
    public void Normalize_RejectsInvalidNumbers(string? input)
    {
        Assert.Null(TurkishPhoneNumber.Normalize(input));
        Assert.False(TurkishPhoneNumber.IsValid(input));
    }
}
