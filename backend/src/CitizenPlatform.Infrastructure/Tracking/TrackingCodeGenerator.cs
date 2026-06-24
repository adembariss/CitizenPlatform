using System.Security.Cryptography;
using CitizenPlatform.Application.Abstractions;

namespace CitizenPlatform.Infrastructure.Tracking;

public sealed class TrackingCodeGenerator : ITrackingCodeGenerator
{
    private const int RandomBytesLength = 3;

    private readonly IDateTimeProvider _dateTimeProvider;

    public TrackingCodeGenerator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        var bytes = RandomNumberGenerator.GetBytes(RandomBytesLength);
        var suffix = Convert.ToHexString(bytes);
        var trackingCode = $"BLD-{_dateTimeProvider.UtcNow.Year}-{suffix}";

        return Task.FromResult(trackingCode);
    }
}
