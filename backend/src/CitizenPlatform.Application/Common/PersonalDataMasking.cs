namespace CitizenPlatform.Application.Common;

public static class PersonalDataMasking
{
    public static string? MaskFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var words = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(MaskWord));
    }

    private static string MaskWord(string word)
    {
        return $"{char.ToUpperInvariant(word[0])}***";
    }
}
