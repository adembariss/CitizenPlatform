namespace CitizenPlatform.Application.Common.Models;

public interface IResult
{
    bool IsSuccess { get; }
}

public sealed record Result(bool IsSuccess, string? Error) : IResult
{
    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Failure(string error)
    {
        return new Result(false, error);
    }
}

public sealed record Result<T>(
    bool IsSuccess,
    T? Value,
    string? Error,
    IReadOnlyCollection<string> Errors) : IResult
{
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null, Array.Empty<string>());
    }

    public static Result<T> Failure(string error, IReadOnlyCollection<string>? errors = null)
    {
        return new Result<T>(false, default, error, errors ?? Array.Empty<string>());
    }
}

/// <summary>
/// Result variant for admin commands/queries that must distinguish "not found or
/// out of tenant scope" (404) from an ordinary validation failure (400).
/// </summary>
public sealed record AdminScopedResult<T>(
    bool IsSuccess,
    bool NotFound,
    T? Value,
    string? Error,
    IReadOnlyCollection<string> Errors) : IResult
{
    public static AdminScopedResult<T> Success(T value)
    {
        return new AdminScopedResult<T>(true, false, value, null, Array.Empty<string>());
    }

    public static AdminScopedResult<T> Failure(string error, IReadOnlyCollection<string>? errors = null)
    {
        return new AdminScopedResult<T>(false, false, default, error, errors ?? Array.Empty<string>());
    }

    public static AdminScopedResult<T> AsNotFound(string error = "Resource not found.")
    {
        return new AdminScopedResult<T>(false, true, default, error, Array.Empty<string>());
    }
}
