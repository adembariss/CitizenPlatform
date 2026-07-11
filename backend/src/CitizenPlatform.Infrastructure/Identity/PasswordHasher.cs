using System.Security.Cryptography;
using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMACSHA256 password hasher. Format: {iterations}.{saltBase64}.{hashBase64}
/// so the iteration count and salt travel with the hash and can be verified without
/// external state.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var segments = passwordHash.Split('.', 3);
        if (segments.Length != 3 || !int.TryParse(segments[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(segments[1]);
            expectedHash = Convert.FromBase64String(segments[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
