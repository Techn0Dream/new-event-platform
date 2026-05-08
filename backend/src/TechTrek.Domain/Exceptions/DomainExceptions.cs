namespace TechTrek.Domain.Exceptions;

/// <summary>
/// Base domain exception. Thrown when a business invariant is violated.
/// These are caught by the global exception middleware and returned as 422/400 responses.
/// They are NOT logged as server errors - they're expected business violations.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
    }
}

/// <summary>Thrown when a requested resource does not exist.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.", "NOT_FOUND") { }

    public NotFoundException(string message)
        : base(message, "NOT_FOUND") { }
}

/// <summary>Thrown when user does not have permission for the requested action.</summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.")
        : base(message, "UNAUTHORIZED") { }
}

/// <summary>Thrown when a business constraint conflict occurs (e.g., duplicate username).</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message, "CONFLICT") { }
}

/// <summary>Thrown when a feature flag is disabled (e.g., freeze_submissions).</summary>
public class FeatureDisabledException : DomainException
{
    public string FeatureKey { get; }

    public FeatureDisabledException(string featureKey)
        : base($"Feature '{featureKey}' is currently disabled.", "FEATURE_DISABLED")
    {
        FeatureKey = featureKey;
    }
}
