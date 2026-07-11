using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.Auth;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class LoginCommandHandlerTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task HandleAsync_WhenCredentialsAreValid_ReturnsAccessToken()
    {
        var user = CreateUser("admin@demo.local", "Demo Admin", UserType.MunicipalityAdmin, "correct-password");
        var userRepository = new FakeUserRepository(user, [new UserRoleAssignment("MUNICIPALITY_ADMIN", "MunicipalityAdmin", MunicipalityId)]);
        var handler = CreateHandler(userRepository);

        var result = await handler.HandleAsync(new LoginCommand("admin@demo.local", "correct-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.AccessToken));
        Assert.Equal("MunicipalityAdmin", result.Value.User.UserType);
        Assert.Equal(MunicipalityId, result.Value.User.MunicipalityId);
        Assert.Contains("MunicipalityAdmin", result.Value.User.Roles);
    }

    [Fact]
    public async Task HandleAsync_WhenPasswordIsIncorrect_ReturnsFailure()
    {
        var user = CreateUser("admin@demo.local", "Demo Admin", UserType.MunicipalityAdmin, "correct-password");
        var userRepository = new FakeUserRepository(user, []);
        var handler = CreateHandler(userRepository);

        var result = await handler.HandleAsync(new LoginCommand("admin@demo.local", "wrong-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsFailureWithoutLeakingWhichPartIsWrong()
    {
        var userRepository = new FakeUserRepository(null, []);
        var handler = CreateHandler(userRepository);

        var result = await handler.HandleAsync(new LoginCommand("unknown@demo.local", "whatever"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsInactive_ReturnsFailure()
    {
        var user = CreateUser("admin@demo.local", "Demo Admin", UserType.MunicipalityAdmin, "correct-password");
        user.Deactivate();
        var userRepository = new FakeUserRepository(user, []);
        var handler = CreateHandler(userRepository);

        var result = await handler.HandleAsync(new LoginCommand("admin@demo.local", "correct-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    private static LoginCommandHandler CreateHandler(IUserRepository userRepository)
    {
        return new LoginCommandHandler(
            new LoginCommandValidator(),
            userRepository,
            new FakeMunicipalityRepository(),
            new FakePasswordHasher(),
            new FakeTokenService());
    }

    private static User CreateUser(string email, string displayName, UserType userType, string plainTextPassword)
    {
        var user = User.Create(email, displayName, userType);
        user.SetPasswordHash($"fake-hash:{plainTextPassword}");
        return user;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;
        private readonly IReadOnlyList<UserRoleAssignment> _roleAssignments;

        public FakeUserRepository(User? user, IReadOnlyList<UserRoleAssignment> roleAssignments)
        {
            _user = user;
            _roleAssignments = roleAssignments;
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
            return Task.FromResult(_roleAssignments);
        }
    }

    private sealed class FakeMunicipalityRepository : IMunicipalityRepository
    {
        public Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Municipality? municipality = id == MunicipalityId ? Municipality.Create("Demo Belediyesi", "DEMO") : null;
            return Task.FromResult(municipality);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"fake-hash:{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == $"fake-hash:{password}";
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessTokenResult CreateAccessToken(AccessTokenRequest request)
        {
            return new AccessTokenResult($"fake-token-for-{request.UserId}", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
