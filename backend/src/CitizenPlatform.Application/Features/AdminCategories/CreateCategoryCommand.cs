namespace CitizenPlatform.Application.Features.AdminCategories;

public sealed record CreateCategoryCommand(string Name, string Code, Guid? MunicipalityId);
