namespace CitizenPlatform.Application.Common.Models;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    IReadOnlyCollection<string> Errors)
{
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>(true, data, message, Array.Empty<string>());
    }

    public static ApiResponse<T> Fail(string message, IReadOnlyCollection<string>? errors = null)
    {
        return new ApiResponse<T>(false, default, message, errors ?? Array.Empty<string>());
    }
}

public sealed record ApiResponse(
    bool Success,
    string? Message,
    IReadOnlyCollection<string> Errors)
{
    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse(true, message, Array.Empty<string>());
    }

    public static ApiResponse Fail(string message, IReadOnlyCollection<string>? errors = null)
    {
        return new ApiResponse(false, message, errors ?? Array.Empty<string>());
    }
}
