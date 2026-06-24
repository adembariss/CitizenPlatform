namespace CitizenPlatform.Application.Common.Models;

public sealed record Result(bool IsSuccess, string? Error)
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
    IReadOnlyCollection<string> Errors)
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
