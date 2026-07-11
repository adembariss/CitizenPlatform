namespace CitizenPlatform.Api.Authorization;

public static class AuthorizationPolicyNames
{
    public const string RequireSystemAdmin = "RequireSystemAdmin";
    public const string RequireMunicipalityAdmin = "RequireMunicipalityAdmin";
    public const string RequireMunicipalityEmployee = "RequireMunicipalityEmployee";
    public const string RequireAdminAccess = "RequireAdminAccess";
}
