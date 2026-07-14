namespace CitizenPlatform.Application.Abstractions;

public interface ISmsSender
{
    /// <summary>
    /// True only for development senders that expose the code to the caller (so the demo
    /// UI can show it). Real SMS providers return false and the code is never revealed.
    /// </summary>
    bool RevealsCode { get; }

    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken);
}
