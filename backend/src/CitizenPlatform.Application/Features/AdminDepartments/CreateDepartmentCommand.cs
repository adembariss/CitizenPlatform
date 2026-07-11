namespace CitizenPlatform.Application.Features.AdminDepartments;

public sealed record CreateDepartmentCommand(string Name, string Code, Guid? MunicipalityId);
