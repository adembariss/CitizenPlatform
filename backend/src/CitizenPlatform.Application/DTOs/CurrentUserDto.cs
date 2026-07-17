namespace CitizenPlatform.Application.DTOs;

public sealed record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    string UserType,
    Guid? MunicipalityId,
    string? MunicipalityName,
    IReadOnlyCollection<string> Roles,
    // Belediye dışı kurum (elektrik/su/doğalgaz) yöneticileri için dolu.
    Guid? InstitutionId = null,
    string? InstitutionName = null);
