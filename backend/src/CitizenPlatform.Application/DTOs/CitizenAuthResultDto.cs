namespace CitizenPlatform.Application.DTOs;

public sealed record CitizenAuthResultDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserDto User,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool PhoneVerified,
    // Sadece geliştirme (gerçek SMS yokken) doldurulur; production'da null.
    string? VerificationCodePreview);
