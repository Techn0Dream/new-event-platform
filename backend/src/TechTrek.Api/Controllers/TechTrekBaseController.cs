using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TechTrek.Shared.Common;

namespace TechTrek.Api.Controllers;

/// <summary>
/// Base controller for all TechTrek API controllers.
/// 
/// Provides:
/// - MediatR dispatch
/// - Standardized ApiResponse wrapping
/// - Request trace ID injection
/// - Common action helpers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class TechTrekBaseController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected TechTrekBaseController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected string? TraceId => HttpContext.TraceIdentifier;

    protected IActionResult OkResult<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Ok(ApiResponse<T>.Ok(result.Value, successMessage ?? "Operation successful", TraceId));

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<T>.NotFound(result.Error!, TraceId)),
            "UNAUTHORIZED" => Unauthorized(ApiResponse<T>.Fail(result.Error!, traceId: TraceId)),
            "CONFLICT" => Conflict(ApiResponse<T>.Fail(result.Error!, traceId: TraceId)),
            _ => BadRequest(ApiResponse<T>.Fail(result.Error!, traceId: TraceId))
        };
    }

    protected IActionResult OkResult(Result result, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Ok(ApiResponse.Ok(successMessage ?? "Operation successful", TraceId));

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse.Fail(result.Error!, traceId: TraceId)),
            "UNAUTHORIZED" => Unauthorized(ApiResponse.Fail(result.Error!, traceId: TraceId)),
            "CONFLICT" => Conflict(ApiResponse.Fail(result.Error!, traceId: TraceId)),
            _ => BadRequest(ApiResponse.Fail(result.Error!, traceId: TraceId))
        };
    }

    protected string? GetClientIp()
        => Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString();

    protected string? GetUserAgent()
        => Request.Headers["User-Agent"].FirstOrDefault();
}
