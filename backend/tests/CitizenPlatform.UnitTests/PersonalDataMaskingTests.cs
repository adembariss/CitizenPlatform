using CitizenPlatform.Application.Common;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class PersonalDataMaskingTests
{
    [Fact]
    public void MaskFullName_WithTwoWordName_MasksEachWord()
    {
        var masked = PersonalDataMasking.MaskFullName("Ada Lovelace");

        Assert.Equal("A*** L***", masked);
    }

    [Fact]
    public void MaskFullName_WithNull_ReturnsNull()
    {
        Assert.Null(PersonalDataMasking.MaskFullName(null));
    }

    [Fact]
    public void MaskFullName_NeverContainsOriginalName()
    {
        var masked = PersonalDataMasking.MaskFullName("Ada Lovelace");

        Assert.DoesNotContain("Ada", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("Lovelace", masked, StringComparison.Ordinal);
    }
}
