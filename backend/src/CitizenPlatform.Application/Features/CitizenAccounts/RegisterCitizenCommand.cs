namespace CitizenPlatform.Application.Features.CitizenAccounts;

public sealed record RegisterCitizenCommand(
    string Email,
    string PhoneNumber,
    string Password,
    string? FullName);
