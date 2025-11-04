namespace AppliedAccountability.Infrastructure.Validation;

/// <summary>
/// Interface for validation service that provides validation operations
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates an object and returns validation result
    /// </summary>
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Validates an object and throws exception if validation fails
    /// </summary>
    Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Validates multiple objects and returns validation results
    /// </summary>
    Task<IEnumerable<ValidationResult>> ValidateManyAsync<T>(IEnumerable<T> instances, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Result of a validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(List<ValidationError> errors) =>
        new() { IsValid = false, Errors = errors };
}

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }

    public ValidationError()
    {
    }

    public ValidationError(string propertyName, string errorMessage, string errorCode = "", object? attemptedValue = null)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        AttemptedValue = attemptedValue;
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public List<ValidationError> Errors { get; }

    public ValidationException(string message, List<ValidationError> errors)
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(List<ValidationError> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}
