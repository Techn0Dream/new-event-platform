namespace TechTrek.Shared.Common;

/// <summary>
/// Result pattern - functional error handling without exceptions for expected failure cases.
/// 
/// WHY: Not every failure should be an exception. Validation failures, "not found" cases,
/// and expected business failures can be modeled as Result objects.
/// This keeps the application layer clean and avoids exception-as-flow-control anti-pattern.
/// 
/// Usage:
///   return Result.Success(data);
///   return Result.Failure("User not found", "NOT_FOUND");
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? error = null, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    public static Result Success() => new(true);
    public static Result Failure(string error, string errorCode = "ERROR") => new(false, error, errorCode);
    public static Result NotFound(string message) => Failure(message, "NOT_FOUND");
    public static Result Unauthorized(string message = "Unauthorized") => Failure(message, "UNAUTHORIZED");
    public static Result Conflict(string message) => Failure(message, "CONFLICT");
}

/// <summary>Generic result with data payload.</summary>
public sealed class Result<T> : Result
{
    private Result(T value) : base(true) { Value = value; }
    private Result(string error, string errorCode) : base(false, error, errorCode) { Value = default!; }

    public T Value { get; }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(string error, string errorCode = "ERROR") => new(error, errorCode);
    public static new Result<T> NotFound(string message) => Failure(message, "NOT_FOUND");
    public static new Result<T> Unauthorized(string message = "Unauthorized") => Failure(message, "UNAUTHORIZED");
    public static new Result<T> Conflict(string message) => Failure(message, "CONFLICT");

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(Value)) : Result<TOut>.Failure(Error!, ErrorCode!);
}
