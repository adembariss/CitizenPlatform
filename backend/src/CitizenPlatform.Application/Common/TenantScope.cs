using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Common;

/// <summary>
/// Centralizes multi-tenant scoping so admin query/command handlers never
/// hand-roll a municipalityId filter. SystemAdmin may see everything (or
/// filter by an explicit municipalityId); every other admin role is locked
/// to their own token municipalityId regardless of what the request asks for.
/// </summary>
public sealed class TenantScope
{
    private TenantScope(bool isSystemAdmin, Guid? municipalityId, bool isAuthorizedAdmin)
    {
        IsSystemAdmin = isSystemAdmin;
        MunicipalityId = municipalityId;
        IsAuthorizedAdmin = isAuthorizedAdmin;
    }

    public bool IsSystemAdmin { get; }

    public Guid? MunicipalityId { get; }

    public bool IsAuthorizedAdmin { get; }

    public static TenantScope From(ICurrentUserService currentUser)
    {
        var isAdmin = currentUser.UserType is UserType.SystemAdmin
            or UserType.MunicipalityAdmin
            or UserType.MunicipalityEmployee;

        return new TenantScope(
            currentUser.IsSystemAdmin,
            currentUser.MunicipalityId,
            isAdmin);
    }

    /// <summary>
    /// Resolves the municipalityId filter to apply to a list/search query.
    /// SystemAdmin: honors the requested filter (null means "all municipalities").
    /// Everyone else: always their own municipalityId, the request value is ignored.
    /// </summary>
    public Guid? ResolveListFilter(Guid? requestedMunicipalityId)
    {
        return IsSystemAdmin ? requestedMunicipalityId : MunicipalityId;
    }

    /// <summary>
    /// True if this scope may read/write data belonging to the given municipality.
    /// </summary>
    public bool CanAccess(Guid municipalityId)
    {
        return IsSystemAdmin || MunicipalityId == municipalityId;
    }
}
