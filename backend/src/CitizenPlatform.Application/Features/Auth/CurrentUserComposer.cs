using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.Auth;

internal static class CurrentUserComposer
{
    public static async Task<CurrentUserDto> ComposeAsync(
        User user,
        IUserRepository userRepository,
        IMunicipalityRepository municipalityRepository,
        IInstitutionRepository institutionRepository,
        CancellationToken cancellationToken)
    {
        var roleAssignments = await userRepository.GetActiveRoleAssignmentsAsync(user.Id, cancellationToken);
        var municipalityId = roleAssignments
            .Select(assignment => assignment.MunicipalityId)
            .FirstOrDefault(id => id is not null);
        var institutionId = roleAssignments
            .Select(assignment => assignment.InstitutionId)
            .FirstOrDefault(id => id is not null);

        string? municipalityName = null;
        if (municipalityId is not null)
        {
            var municipality = await municipalityRepository.GetByIdAsync(municipalityId.Value, cancellationToken);
            municipalityName = municipality?.Name;
        }

        string? institutionName = null;
        if (institutionId is not null)
        {
            var institution = await institutionRepository.GetByIdAsync(institutionId.Value, cancellationToken);
            institutionName = institution?.Name;
        }

        var roles = roleAssignments
            .Select(assignment => assignment.RoleName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new CurrentUserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.UserType.ToString(),
            municipalityId,
            // Kurum yöneticisinde belediye adı yerine kurum adını göster (panel başlığı için).
            municipalityName ?? institutionName,
            roles,
            institutionId,
            institutionName);
    }
}
