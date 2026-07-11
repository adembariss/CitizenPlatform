namespace CitizenPlatform.Application.DTOs;

public sealed record LoginResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserDto User);
