namespace TechTrek.Shared.Common;

/// <summary>
/// Standardized API response wrapper.
/// ALL API endpoints return this shape - frontend can rely on consistent structure.
/// 
/// Matches the format specified in requirements:
/// { "success": true, "message": "...", "data": {}, "errors": [], "timestamp": "...", "traceId": "..." }
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IList<string> Errors { get; init; } = new List<string>();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Operation successful", string? traceId = null)
        => new() { Success = true, Message = message, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(string message, IList<string>? errors = null, string? traceId = null)
        => new() { Success = false, Message = message, Errors = errors ?? new List<string>(), TraceId = traceId };

    public static ApiResponse<T> NotFound(string message, string? traceId = null)
        => new() { Success = false, Message = message, Errors = new[] { "Resource not found" }, TraceId = traceId };
}

/// <summary>Non-generic variant for operations that return no data body.</summary>
public sealed class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IList<string> Errors { get; init; } = new List<string>();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? TraceId { get; init; }

    public static ApiResponse Ok(string message = "Operation successful", string? traceId = null)
        => new() { Success = true, Message = message, TraceId = traceId };

    public static ApiResponse Fail(string message, IList<string>? errors = null, string? traceId = null)
        => new() { Success = false, Message = message, Errors = errors ?? new List<string>(), TraceId = traceId };
}
