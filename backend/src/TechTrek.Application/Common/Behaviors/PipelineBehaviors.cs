using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TechTrek.Shared.Common;

namespace TechTrek.Application.Common.Behaviors;

/// <summary>
/// Validation Pipeline Behavior
/// 
/// Sits in the MediatR pipeline BEFORE handlers execute.
/// All FluentValidation validators for a command are run here.
/// If any validation fails, the handler never executes.
/// 
/// This replaces the need for validation in every handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0) return await next();

        // Validation failed - don't execute handler
        throw new Domain.Exceptions.DomainException(
            string.Join("; ", failures.Select(f => f.ErrorMessage)),
            "VALIDATION_ERROR");
    }
}

/// <summary>
/// Logging Pipeline Behavior
/// 
/// Logs every command/query execution with timing information.
/// Slow queries (> 500ms) are logged as warnings.
/// Exceptions are logged as errors with full context.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("[CQRS] Handling {RequestName}", requestName);
            var response = await next();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (elapsed > 500)
                _logger.LogWarning("[CQRS] SLOW {RequestName} took {ElapsedMs}ms", requestName, elapsed);
            else
                _logger.LogInformation("[CQRS] Completed {RequestName} in {ElapsedMs}ms", requestName, elapsed);

            return response;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[CQRS] Failed {RequestName} after {ElapsedMs}ms", requestName, elapsed);
            throw;
        }
    }
}
