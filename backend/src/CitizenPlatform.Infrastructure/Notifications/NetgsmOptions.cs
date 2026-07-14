namespace CitizenPlatform.Infrastructure.Notifications;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>"Log" (varsayılan, geliştirme) veya "Netgsm".</summary>
    public string Provider { get; init; } = "Log";

    public NetgsmOptions Netgsm { get; init; } = new();
}

public sealed class NetgsmOptions
{
    public string ApiBaseUrl { get; init; } = "https://api.netgsm.com.tr";

    public string UserCode { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string MsgHeader { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UserCode)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(MsgHeader);
}
