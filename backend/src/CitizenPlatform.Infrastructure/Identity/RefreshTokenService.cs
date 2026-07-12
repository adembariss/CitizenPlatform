using System.Security.Cryptography;
using System.Text;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Identity;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly CitizenPlatformDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenService(
        CitizenPlatformDbContext dbContext,
        JwtOptions jwtOptions,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (entity, plainToken) = CreateToken(userId);

        await _dbContext.RefreshTokens.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(plainToken, entity.ExpiresAt);
    }

    public async Task<RotatedRefreshToken?> RotateAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var current = await FindByPlainTokenAsync(refreshToken, cancellationToken);
        if (current is null || !current.IsActive)
        {
            return null;
        }

        var (next, plainToken) = CreateToken(current.UserId);

        await _dbContext.RefreshTokens.AddAsync(next, cancellationToken);
        current.Revoke(next.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RotatedRefreshToken(current.UserId, plainToken, next.ExpiresAt);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var current = await FindByPlainTokenAsync(refreshToken, cancellationToken);
        if (current is null || current.RevokedAt is not null)
        {
            return;
        }

        current.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private (RefreshToken Entity, string PlainToken) CreateToken(Guid userId)
    {
        var plainToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(48)).ToLowerInvariant();
        var expiresAt = _dateTimeProvider.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
        var entity = RefreshToken.Create(userId, HashToken(plainToken), expiresAt);

        return (entity, plainToken);
    }

    private Task<RefreshToken?> FindByPlainTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        return _dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    private static string HashToken(string plainToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken))).ToLowerInvariant();
    }
}
