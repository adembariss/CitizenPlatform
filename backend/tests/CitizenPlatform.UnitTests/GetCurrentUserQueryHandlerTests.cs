using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.Auth;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class GetCurrentUserQueryHandlerTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task HandleAsync_WhenUserExists_ReturnsCurrentUserWithRolesAndMunicipality()
    {
        var user = User.Create("employee@demo.local", "Demo Employee", UserType.MunicipalityEmployee);
        var roleAssignments = new List<UserRoleAssignment>
        {
            new("MUNICIPALITY_EMPLOYEE", "MunicipalityEmployee", MunicipalityId)
        };
        var handler = new GetCurrentUserQueryHandler(
            new FakeUserRepository(user, roleAssignments),
            new FakeMunicipalityRepository(),
            new FakeInstitutionRepository());

        var result = await handler.HandleAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("employee@demo.local", result.Value!.Email);
        Assert.Equal("MunicipalityEmployee", result.Value.UserType);
        Assert.Equal(MunicipalityId, result.Value.MunicipalityId);
        Assert.Equal("Demo Belediyesi", result.Value.MunicipalityName);
        Assert.Contains("MunicipalityEmployee", result.Value.Roles);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsFailure()
    {
        var handler = new GetCurrentUserQueryHandler(
            new FakeUserRepository(null, []),
            new FakeMunicipalityRepository(),
            new FakeInstitutionRepository());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WhenSystemAdmin_HasNoMunicipality()
    {
        var user = User.Create("systemadmin@demo.local", "Sistem Admin", UserType.SystemAdmin);
        var roleAssignments = new List<UserRoleAssignment>
        {
            new("SYSTEM_ADMIN", "SystemAdmin", null)
        };
        var handler = new GetCurrentUserQueryHandler(
            new FakeUserRepository(user, roleAssignments),
            new FakeMunicipalityRepository(),
            new FakeInstitutionRepository());

        var result = await handler.HandleAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.MunicipalityId);
        Assert.Null(result.Value.MunicipalityName);
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

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user);
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

    private sealed class FakeInstitutionRepository : IInstitutionRepository
    {
        public Task<Institution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<Institution?>(null);

        public Task<IReadOnlyList<Institution>> ListByAreaAsync(string province, string? district, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Institution>>(System.Array.Empty<Institution>());
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
}
