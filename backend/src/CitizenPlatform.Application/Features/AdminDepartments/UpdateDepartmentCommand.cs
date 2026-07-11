namespace CitizenPlatform.Application.Features.AdminDepartments;

public sealed record UpdateDepartmentCommand(Guid Id, string? Name, bool? IsActive);
