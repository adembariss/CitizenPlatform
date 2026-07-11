namespace CitizenPlatform.Application.DTOs;

public sealed record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    string UserType,
    Guid? MunicipalityId,
    string? MunicipalityName,
    IReadOnlyCollection<string> Roles);
