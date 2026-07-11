using System.Security.Claims;
using CitizenPlatform.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CitizenPlatform.IntegrationTests;

/// <summary>
/// Exercises the real ASP.NET Core IAuthorizationService against the exact policy
/// definitions the API uses (AuthorizationPolicySetup), without needing a live database
/// or HTTP host. This verifies the same authorization decisions the [Authorize] attributes
/// on the admin controllers rely on: no token/no role -> denied, Citizen -> denied,
/// employee/admin/system-admin hierarchy honored.
/// </summary>
public sealed class AdminAuthorizationPolicyTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenUserIsNotAuthenticated_DeniesAdminAccess()
    {
        var authorizationService = BuildAuthorizationService();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await authorizationService.AuthorizeAsync(anonymous, AuthorizationPolicyNames.RequireAdminAccess);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasCitizenRoleOnly_DeniesAdminAccess()
    {
        var authorizationService = BuildAuthorizationService();
        var citizen = BuildAuthenticatedPrincipal("Citizen");

        var result = await authorizationService.AuthorizeAsync(citizen, AuthorizationPolicyNames.RequireAdminAccess);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsMunicipalityEmployee_AllowsAdminAccessButNotMunicipalityAdmin()
    {
        var authorizationService = BuildAuthorizationService();
        var employee = BuildAuthenticatedPrincipal("MunicipalityEmployee");

        var adminAccess = await authorizationService.AuthorizeAsync(employee, AuthorizationPolicyNames.RequireAdminAccess);
        var municipalityAdmin = await authorizationService.AuthorizeAsync(employee, AuthorizationPolicyNames.RequireMunicipalityAdmin);
        var systemAdmin = await authorizationService.AuthorizeAsync(employee, AuthorizationPolicyNames.RequireSystemAdmin);

        Assert.True(adminAccess.Succeeded);
        Assert.False(municipalityAdmin.Succeeded);
        Assert.False(systemAdmin.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsMunicipalityAdmin_AllowsAdminAccessAndMunicipalityAdminButNotSystemAdmin()
    {
        var authorizationService = BuildAuthorizationService();
        var admin = BuildAuthenticatedPrincipal("MunicipalityAdmin");

        var adminAccess = await authorizationService.AuthorizeAsync(admin, AuthorizationPolicyNames.RequireAdminAccess);
        var municipalityAdmin = await authorizationService.AuthorizeAsync(admin, AuthorizationPolicyNames.RequireMunicipalityAdmin);
        var systemAdmin = await authorizationService.AuthorizeAsync(admin, AuthorizationPolicyNames.RequireSystemAdmin);

        Assert.True(adminAccess.Succeeded);
        Assert.True(municipalityAdmin.Succeeded);
        Assert.False(systemAdmin.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsSystemAdmin_AllowsEveryAdminPolicy()
    {
        var authorizationService = BuildAuthorizationService();
        var systemAdminUser = BuildAuthenticatedPrincipal("SystemAdmin");

        var adminAccess = await authorizationService.AuthorizeAsync(systemAdminUser, AuthorizationPolicyNames.RequireAdminAccess);
        var municipalityAdmin = await authorizationService.AuthorizeAsync(systemAdminUser, AuthorizationPolicyNames.RequireMunicipalityAdmin);
        var municipalityEmployee = await authorizationService.AuthorizeAsync(systemAdminUser, AuthorizationPolicyNames.RequireMunicipalityEmployee);
        var systemAdmin = await authorizationService.AuthorizeAsync(systemAdminUser, AuthorizationPolicyNames.RequireSystemAdmin);

        Assert.True(adminAccess.Succeeded);
        Assert.True(municipalityAdmin.Succeeded);
        Assert.True(municipalityEmployee.Succeeded);
        Assert.True(systemAdmin.Succeeded);
    }

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore(AuthorizationPolicySetup.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal BuildAuthenticatedPrincipal(params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
