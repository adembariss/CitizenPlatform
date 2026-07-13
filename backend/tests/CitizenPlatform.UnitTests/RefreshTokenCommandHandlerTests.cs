using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.Auth;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task HandleAsync_WhenTokenIsUnknown_ReturnsFailure()
    {
        var handler = CreateHandler(user: null, refreshTokenService: new FakeRefreshTokenService(rotationResult: null));

        var result = await handler.HandleAsync(new RefreshTokenCommand("unknown-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired refresh token.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenIsBlank_ReturnsFailureWithoutRotating()
    {
        var refreshTokenService = new FakeRefreshTokenService(rotationResult: null);
        var handler = CreateHandler(user: null, refreshTokenService);

        var result = await handler.HandleAsync(new RefreshTokenCommand("   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(refreshTokenService.RotateWasCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenIsValid_ReturnsNewAccessAndRotatedRefreshToken()
    {
        var user = CreateUser();
        var rotation = new RotatedRefreshToken(user.Id, "rotated-refresh-token", DateTimeOffset.UtcNow.AddDays(14));
        var refreshTokenService = new FakeRefreshTokenService(rotation);
        var handler = CreateHandler(user, refreshTokenService);

        var result = await handler.HandleAsync(new RefreshTokenCommand("valid-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("rotated-refresh-token", result.Value!.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.Equal("MunicipalityAdmin", result.Value.User.UserType);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsInactive_FailsAndRevokesTheRotatedToken()
    {
        var user = CreateUser();
        user.Deactivate();
        var rotation = new RotatedRefreshToken(user.Id, "rotated-refresh-token", DateTimeOffset.UtcNow.AddDays(14));
        var refreshTokenService = new FakeRefreshTokenService(rotation);
        var handler = CreateHandler(user, refreshTokenService);

        var result = await handler.HandleAsync(new RefreshTokenCommand("valid-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("rotated-refresh-token", refreshTokenService.LastRevokedToken);
    }

    private static RefreshTokenCommandHandler CreateHandler(User? user, FakeRefreshTokenService refreshTokenService)
    {
        return new RefreshTokenCommandHandler(
            refreshTokenService,
            new FakeUserRepository(user),
            new FakeMunicipalityRepository(),
            new FakeTokenService());
    }

    private static User CreateUser()
    {
        var user = User.Create("admin@demo.local", "Demo Admin", UserType.MunicipalityAdmin);
        user.SetPasswordHash("fake-hash");
        return user;
    }

    private sealed class FakeRefreshTokenService : IRefreshTokenService
    {
        private readonly RotatedRefreshToken? _rotationResult;

        public FakeRefreshTokenService(RotatedRefreshToken? rotationResult)
        {
            _rotationResult = rotationResult;
        }

        public bool RotateWasCalled { get; private set; }

        public string? LastRevokedToken { get; private set; }

        public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new IssuedRefreshToken("issued-token", DateTimeOffset.UtcNow.AddDays(14)));
        }

        public Task<RotatedRefreshToken?> RotateAsync(string refreshToken, CancellationToken cancellationToken)
        {
            RotateWasCalled = true;
            return Task.FromResult(_rotationResult);
        }

        public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LastRevokedToken = refreshToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user)
        {
            _user = user;
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user is not null && _user.Email == email.Trim().ToLowerInvariant() ? _user : null);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user is not null && _user.Id == id ? _user : null);
        }

        public Task<IReadOnlyList<UserRoleAssignment>> GetActiveRoleAssignmentsAsync(Guid userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<UserRoleAssignment> assignments =
                [new UserRoleAssignment("MUNICIPALITY_ADMIN", "MunicipalityAdmin", MunicipalityId)];
            return Task.FromResult(assignments);
        }
    }

    private sealed class FakeMunicipalityRepository : IMunicipalityRepository
    {
        public Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Municipality? municipality = id == MunicipalityId ? Municipality.Create("Demo Belediyesi", "DEMO") : null;
            return Task.FromResult(municipality);
        }

        public Task<IReadOnlyList<string>> GetProvincesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<IReadOnlyList<DistrictRow>> GetDistrictsByProvinceAsync(string province, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DistrictRow>>(Array.Empty<DistrictRow>());
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessTokenResult CreateAccessToken(AccessTokenRequest request)
        {
            return new AccessTokenResult($"fake-token-for-{request.UserId}", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
