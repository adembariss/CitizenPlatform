using CitizenPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CitizenPlatform.Infrastructure.Notifications;

/// <summary>
/// Netgsm HTTP API (sms/send/get) üzerinden gerçek SMS gönderimi. Kod asla ifşa edilmez
/// (RevealsCode=false). Yapılandırma: Sms:Provider=Netgsm + Sms:Netgsm:UserCode/Password/MsgHeader.
/// </summary>
public sealed class NetgsmSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly NetgsmOptions _options;
    private readonly ILogger<NetgsmSmsSender> _logger;

    public NetgsmSmsSender(HttpClient httpClient, NetgsmOptions options, ILogger<NetgsmSmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public bool RevealsCode => false;

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        var gsmNo = ToNetgsmNumber(phoneNumber);
        if (gsmNo is null)
        {
            _logger.LogWarning("Netgsm: geçersiz telefon numarası, SMS gönderilmedi.");
            return;
        }

        var url =
            $"{_options.ApiBaseUrl.TrimEnd('/')}/sms/send/get" +
            $"?usercode={Uri.EscapeDataString(_options.UserCode)}" +
            $"&password={Uri.EscapeDataString(_options.Password)}" +
            $"&gsmno={Uri.EscapeDataString(gsmNo)}" +
            $"&message={Uri.EscapeDataString(message)}" +
            $"&msgheader={Uri.EscapeDataString(_options.MsgHeader)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        // Netgsm başarı kodları "00" / "01" ile başlar; aksi halde hata kodudur.
        if (!response.IsSuccessStatusCode || !(body.StartsWith("00") || body.StartsWith("01")))
        {
            _logger.LogError("Netgsm SMS gönderimi başarısız. Yanıt: {Response}", body);
            return;
        }

        _logger.LogInformation("Netgsm SMS gönderildi.");
    }

    // +905XXXXXXXXX / 05XXXXXXXXX / 5XXXXXXXXX -> 5XXXXXXXXX (Netgsm 10 haneli bekler)
    private static string? ToNetgsmNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = digits[1..];
        }

        return digits.Length == 10 && digits[0] == '5' ? digits : null;
    }
}
