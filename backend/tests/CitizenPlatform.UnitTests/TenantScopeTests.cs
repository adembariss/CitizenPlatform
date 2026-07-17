using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Domain.Enums;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class TenantScopeTests
{
    private static readonly Guid OwnMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMunicipalityId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void ResolveListFilter_ForMunicipalityEmployee_AlwaysUsesOwnMunicipality()
    {
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityEmployee, OwnMunicipalityId));

        var resolved = scope.ResolveListFilter(OtherMunicipalityId);

        Assert.Equal(OwnMunicipalityId, resolved);
    }

    [Fact]
    public void ResolveListFilter_ForSystemAdmin_HonorsRequestedFilter()
    {
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, municipalityId: null));

        var resolved = scope.ResolveListFilter(OtherMunicipalityId);

        Assert.Equal(OtherMunicipalityId, resolved);
    }

    [Fact]
    public void ResolveListFilter_ForSystemAdmin_WithNoRequestedFilter_ReturnsNullMeaningAllMunicipalities()
    {
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, municipalityId: null));

        var resolved = scope.ResolveListFilter(null);

        Assert.Null(resolved);
    }

    [Fact]
    public void CanAccess_ForMunicipalityAdmin_DeniesOtherMunicipality()
    {
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.MunicipalityAdmin, OwnMunicipalityId));

        Assert.True(scope.CanAccess(OwnMunicipalityId));
        Assert.False(scope.CanAccess(OtherMunicipalityId));
    }

    [Fact]
    public void CanAccess_ForSystemAdmin_AllowsAnyMunicipality()
    {
        var scope = TenantScope.From(new FakeCurrentUserService(UserType.SystemAdmin, municipalityId: null));

        Assert.True(scope.CanAccess(OwnMunicipalityId));
        Assert.True(scope.CanAccess(OtherMunicipalityId));
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(UserType userType, Guid? municipalityId)
        {
            UserType = userType;
            MunicipalityId = municipalityId;
        }

        public string? UserId => Guid.NewGuid().ToString();

        public Guid? UserGuid => Guid.Parse(UserId!);

        public bool IsAuthenticated => true;

        public Guid? MunicipalityId { get; }
        public Guid? InstitutionId { get; }

        public UserType? UserType { get; }

        public IReadOnlyCollection<string> Roles => [UserType?.ToString() ?? string.Empty];

        public bool IsSystemAdmin => UserType == CitizenPlatform.Domain.Enums.UserType.SystemAdmin;
    }
}
