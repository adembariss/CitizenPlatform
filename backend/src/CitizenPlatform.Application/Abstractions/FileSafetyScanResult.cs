namespace CitizenPlatform.Application.Abstractions;

public sealed record FileSafetyScanResult(bool IsSafe, string? FailureReason)
{
    public static FileSafetyScanResult Safe()
    {
        return new FileSafetyScanResult(true, null);
    }

    public static FileSafetyScanResult Unsafe(string reason)
    {
        return new FileSafetyScanResult(false, reason);
    }
}
