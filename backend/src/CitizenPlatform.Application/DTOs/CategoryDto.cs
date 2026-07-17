namespace CitizenPlatform.Application.DTOs;

public sealed record CategoryDto(Guid Id, Guid? MunicipalityId, string Name, string Code, bool IsActive);

// MunicipalityId belediye birimlerinde, InstitutionId kurum birimlerinde doludur (biri null).
public sealed record DepartmentDto(Guid Id, Guid? MunicipalityId, string Name, string Code, bool IsActive, Guid? InstitutionId = null);
