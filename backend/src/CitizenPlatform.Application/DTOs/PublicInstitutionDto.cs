namespace CitizenPlatform.Application.DTOs;

/// <summary>Vatandaşın konumuna göre şikayet edebileceği dağıtım kurumu.</summary>
public sealed record PublicInstitutionDto(Guid Id, string Name, string Type, string TypeLabel);
