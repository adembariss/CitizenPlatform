using Microsoft.AspNetCore.Authorization;

namespace CitizenPlatform.Api.Authorization;

/// <summary>
/// Central definition of the admin-side authorization policies so both the real
/// startup wiring (ServiceCollectionExtensions) and tests configure the exact
/// same rules - no duplicated/divergent policy logic.
/// </summary>
public static class AuthorizationPolicySetup
{
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicyNames.RequireSystemAdmin, policy =>
            policy.RequireRole("SystemAdmin"));

        options.AddPolicy(AuthorizationPolicyNames.RequireMunicipalityAdmin, policy =>
            policy.RequireRole("SystemAdmin", "MunicipalityAdmin"));

        options.AddPolicy(AuthorizationPolicyNames.RequireMunicipalityEmployee, policy =>
            policy.RequireRole("SystemAdmin", "MunicipalityAdmin", "MunicipalityEmployee"));

        options.AddPolicy(AuthorizationPolicyNames.RequireAdminAccess, policy =>
            policy.RequireRole("SystemAdmin", "MunicipalityAdmin", "MunicipalityEmployee"));

        // Citizens have no roles; they are identified by the user_type claim.
        options.AddPolicy(AuthorizationPolicyNames.RequireCitizen, policy =>
            policy.RequireClaim("user_type", "Citizen"));
    }
}
