namespace CitizenPlatform.Application.Abstractions;

public sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);

public sealed record RotatedRefreshToken(Guid UserId, string Token, DateTimeOffset ExpiresAt);

public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the given refresh token and, when it is active, revokes it and returns a
    /// freshly issued replacement (single-use rotation). Returns null for unknown, expired
    /// or already revoked tokens.
    /// </summary>
    Task<RotatedRefreshToken?> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
