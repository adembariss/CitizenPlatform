namespace CitizenPlatform.Application.Features.AdminCategories;

public sealed record UpdateCategoryCommand(Guid Id, string? Name, bool? IsActive);
