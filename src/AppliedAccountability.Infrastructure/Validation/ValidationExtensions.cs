using System.Text.RegularExpressions;

namespace AppliedAccountability.Infrastructure.Validation;

/// <summary>
/// Common validation extension methods for enterprise validation scenarios
/// </summary>
public static class ValidationExtensions
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    private static readonly Regex UrlRegex = new(
        @"^https?://[^\s/$.?#].[^\s]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates that a string is not null or whitespace
    /// </summary>
    public static ValidationResult ValidateRequired(this string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} is required", "REQUIRED")
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a string has a minimum length
    /// </summary>
    public static ValidationResult ValidateMinLength(this string? value, string propertyName, int minLength)
    {
        if (value == null || value.Length < minLength)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be at least {minLength} characters", "MIN_LENGTH", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a string has a maximum length
    /// </summary>
    public static ValidationResult ValidateMaxLength(this string? value, string propertyName, int maxLength)
    {
        if (value != null && value.Length > maxLength)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must not exceed {maxLength} characters", "MAX_LENGTH", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a string is a valid email address
    /// </summary>
    public static ValidationResult ValidateEmail(this string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value) || !EmailRegex.IsMatch(value))
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be a valid email address", "INVALID_EMAIL", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a string is a valid phone number (E.164 format)
    /// </summary>
    public static ValidationResult ValidatePhone(this string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value) || !PhoneRegex.IsMatch(value))
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be a valid phone number", "INVALID_PHONE", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a string is a valid URL
    /// </summary>
    public static ValidationResult ValidateUrl(this string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value) || !UrlRegex.IsMatch(value))
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be a valid URL", "INVALID_URL", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a number is within a range
    /// </summary>
    public static ValidationResult ValidateRange(this int value, string propertyName, int min, int max)
    {
        if (value < min || value > max)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be between {min} and {max}", "OUT_OF_RANGE", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a decimal is within a range
    /// </summary>
    public static ValidationResult ValidateRange(this decimal value, string propertyName, decimal min, decimal max)
    {
        if (value < min || value > max)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be between {min} and {max}", "OUT_OF_RANGE", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a date is not in the past
    /// </summary>
    public static ValidationResult ValidateFutureDate(this DateTime value, string propertyName)
    {
        if (value < DateTime.UtcNow)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be a future date", "PAST_DATE", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a date is not in the future
    /// </summary>
    public static ValidationResult ValidatePastDate(this DateTime value, string propertyName)
    {
        if (value > DateTime.UtcNow)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must be a past date", "FUTURE_DATE", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a collection is not empty
    /// </summary>
    public static ValidationResult ValidateNotEmpty<T>(this IEnumerable<T>? collection, string propertyName)
    {
        if (collection == null || !collection.Any())
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must contain at least one item", "EMPTY_COLLECTION")
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a GUID is not empty
    /// </summary>
    public static ValidationResult ValidateNotEmpty(this Guid value, string propertyName)
    {
        if (value == Guid.Empty)
        {
            return ValidationResult.Failure(new List<ValidationError>
            {
                new(propertyName, $"{propertyName} must not be empty", "EMPTY_GUID", value)
            });
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Combines multiple validation results
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allErrors = results
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        return allErrors.Any()
            ? ValidationResult.Failure(allErrors)
            : ValidationResult.Success();
    }
}
