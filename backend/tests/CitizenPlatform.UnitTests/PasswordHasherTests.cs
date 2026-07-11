using CitizenPlatform.Infrastructure.Identity;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_WhenPasswordMatches_ReturnsTrue()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Demo123!");

        Assert.True(hasher.Verify("Demo123!", hash));
    }

    [Fact]
    public void Verify_WhenPasswordDoesNotMatch_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Demo123!");

        Assert.False(hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_NeverProducesPlainTextPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Demo123!");

        Assert.DoesNotContain("Demo123!", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Hash_ProducesDifferentSaltEachTime()
    {
        var hasher = new PasswordHasher();

        var first = hasher.Hash("Demo123!");
        var second = hasher.Hash("Demo123!");

        Assert.NotEqual(first, second);
    }
}
