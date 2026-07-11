namespace CitizenPlatform.Application.DTOs;

public sealed record CategoryDto(Guid Id, Guid? MunicipalityId, string Name, string Code, bool IsActive);

public sealed record DepartmentDto(Guid Id, Guid MunicipalityId, string Name, string Code, bool IsActive);
