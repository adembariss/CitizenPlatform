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
