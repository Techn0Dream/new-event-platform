using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TechTrek.Domain.Exceptions;
using TechTrek.Shared.Common;

namespace TechTrek.Api.Middleware;

/// <summary>
/// Global Exception Handling Middleware
/// 
/// Catches all unhandled exceptions and returns consistent ApiResponse<object> shapes.
/// 
/// Classification:
/// - DomainException → 422 Unprocessable Entity (expected business violation)
/// - NotFoundException → 404 Not Found
/// - UnauthorizedException → 401 Unauthorized
/// - ConflictException → 409 Conflict
/// - FeatureDisabledException → 503 Service Unavailable
/// - ValidationError → 400 Bad Request
/// - All others → 500 Internal Server Error (sanitized message in production)
/// </summary>
public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, message, errorCode) = exception switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message, e.Code),
            UnauthorizedException e => (HttpStatusCode.Unauthorized, e.Message, e.Code),
            ConflictException e => (HttpStatusCode.Conflict, e.Message, e.Code),
            FeatureDisabledException e => (HttpStatusCode.ServiceUnavailable, e.Message, e.Code),
            DomainException e when e.Code == "VALIDATION_ERROR" => (HttpStatusCode.BadRequest, e.Message, e.Code),
            DomainException e => (HttpStatusCode.UnprocessableEntity, e.Message, e.Code),
            OperationCanceledException => (HttpStatusCode.RequestTimeout, "The request was cancelled.", "REQUEST_CANCELLED"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.", "INTERNAL_ERROR")
        };

        // Only log 5xx as errors
        if ((int)statusCode >= 500)
        {
            _logger.LogError(exception,
                "Unhandled exception [{ErrorCode}] {Message} | TraceId: {TraceId}",
                errorCode, exception.Message, traceId);
        }
        else
        {
            _logger.LogWarning("Domain exception [{ErrorCode}] {Message} | TraceId: {TraceId}",
                errorCode, exception.Message, traceId);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var errors = _environment.IsDevelopment() && (int)statusCode >= 500
            ? new[] { exception.ToString() } // Show stack trace in dev
            : new[] { message };

        var response = new
        {
            success = false,
            message,
            errors,
            timestamp = DateTime.UtcNow,
            traceId,
            code = errorCode
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
