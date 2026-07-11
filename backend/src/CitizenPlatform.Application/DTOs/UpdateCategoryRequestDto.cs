namespace CitizenPlatform.Application.DTOs;

public sealed record UpdateCategoryRequestDto(string? Name, bool? IsActive);

public sealed record UpdateDepartmentRequestDto(string? Name, bool? IsActive);
