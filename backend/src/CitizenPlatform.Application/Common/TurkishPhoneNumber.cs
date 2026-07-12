namespace CitizenPlatform.Application.Common;

/// <summary>
/// Türkiye cep telefonu numaralarını doğrular ve E.164 (+905XXXXXXXXX) biçimine getirir.
/// Kabul edilen girişler: "0532 123 45 67", "05321234567", "5321234567",
/// "+90 532 123 45 67", "+905321234567" vb. Geçersizse null döner.
/// </summary>
public static class TurkishPhoneNumber
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = digits[1..];
        }

        // Geçerli Türk cep numarası: 10 hane ve 5 ile başlar.
        if (digits.Length == 10 && digits[0] == '5')
        {
            return "+90" + digits;
        }

        return null;
    }

    public static bool IsValid(string? input) => Normalize(input) is not null;
}
