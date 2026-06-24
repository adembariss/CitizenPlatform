namespace CitizenPlatform.Application.Abstractions;

public interface ITrackingCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken);
}
