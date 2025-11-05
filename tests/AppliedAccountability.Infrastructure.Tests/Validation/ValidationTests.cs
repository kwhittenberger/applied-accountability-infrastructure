using AppliedAccountability.Infrastructure.Validation;

namespace AppliedAccountability.Infrastructure.Tests.Validation;

public class ValidationResultTests
{
    #region Success Tests

    [Fact]
    public void Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_CreatesNewInstanceEachTime()
    {
        // Act
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();

        // Assert
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region Failure Tests

    [Fact]
    public void Failure_WithErrors_CreatesInvalidResult()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Email is invalid")
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Failure_WithEmptyErrors_CreatesInvalidResult()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var result = new ValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Constructor_AllowsSettingProperties()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Field", "Error")
        };

        // Act
        var result = new ValidationResult
        {
            IsValid = true,
            Errors = errors
        };

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Errors);
    }

    #endregion
}

public class ValidationErrorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_InitializesWithEmptyStrings()
    {
        // Act
        var error = new ValidationError();

        // Assert
        Assert.NotNull(error);
        Assert.Equal(string.Empty, error.PropertyName);
        Assert.Equal(string.Empty, error.ErrorMessage);
        Assert.Equal(string.Empty, error.ErrorCode);
        Assert.Null(error.AttemptedValue);
    }

    [Fact]
    public void Constructor_WithPropertyAndMessage_SetsProperties()
    {
        // Act
        var error = new ValidationError("Email", "Email is required");

        // Assert
        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Email is required", error.ErrorMessage);
        Assert.Equal(string.Empty, error.ErrorCode);
        Assert.Null(error.AttemptedValue);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var attemptedValue = "invalid@";

        // Act
        var error = new ValidationError("Email", "Email format is invalid", "EMAIL_INVALID", attemptedValue);

        // Assert
        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Email format is invalid", error.ErrorMessage);
        Assert.Equal("EMAIL_INVALID", error.ErrorCode);
        Assert.Equal(attemptedValue, error.AttemptedValue);
    }

    [Fact]
    public void Constructor_WithNullAttemptedValue_AllowsNull()
    {
        // Act
        var error = new ValidationError("Name", "Name is required", "NAME_REQUIRED", null);

        // Assert
        Assert.Null(error.AttemptedValue);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var error = new ValidationError();

        // Act
        error.PropertyName = "Age";
        error.ErrorMessage = "Age must be positive";
        error.ErrorCode = "AGE_INVALID";
        error.AttemptedValue = -5;

        // Assert
        Assert.Equal("Age", error.PropertyName);
        Assert.Equal("Age must be positive", error.ErrorMessage);
        Assert.Equal("AGE_INVALID", error.ErrorCode);
        Assert.Equal(-5, error.AttemptedValue);
    }

    #endregion
}

public class ValidationExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMessageAndErrors_SetsProperties()
    {
        // Arrange
        var message = "Validation failed with 2 errors";
        var errors = new List<ValidationError>
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Email is invalid")
        };

        // Act
        var exception = new ValidationException(message, errors);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void Constructor_WithErrorsOnly_UsesDefaultMessage()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Name", "Name is required")
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        Assert.Equal("Validation failed", exception.Message);
        Assert.Single(exception.Errors);
    }

    [Fact]
    public void Constructor_WithEmptyErrors_CreatesException()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act
        var exception = new ValidationException(errors);

        // Assert
        Assert.NotNull(exception);
        Assert.Empty(exception.Errors);
    }

    #endregion

    #region Throw Tests

    [Fact]
    public void Throw_WithValidationException_CanBeCaught()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Field", "Error")
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() =>
        {
            throw new ValidationException(errors);
        });

        Assert.Single(exception.Errors);
    }

    [Fact]
    public void Throw_WithValidationException_PreservesErrors()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Name", "Name is required", "NAME_REQUIRED", null),
            new ValidationError("Email", "Email is invalid", "EMAIL_INVALID", "bad-email")
        };

        // Act
        ValidationException? caughtException = null;
        try
        {
            throw new ValidationException("Multiple errors", errors);
        }
        catch (ValidationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.Equal(2, caughtException.Errors.Count);
        Assert.Equal("Name", caughtException.Errors[0].PropertyName);
        Assert.Equal("Email", caughtException.Errors[1].PropertyName);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ValidationWorkflow_CreateErrorsAndThrow_WorksCorrectly()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Simulate validation
        if (string.IsNullOrEmpty(""))
            errors.Add(new ValidationError("Username", "Username is required", "USERNAME_REQUIRED"));

        if (!"invalid@".Contains("@."))
            errors.Add(new ValidationError("Email", "Email format is invalid", "EMAIL_INVALID", "invalid@"));

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() =>
        {
            if (errors.Count > 0)
                throw new ValidationException(errors);
        });

        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void ValidationWorkflow_SuccessPath_NoExceptionThrown()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Simulate successful validation
        var username = "validuser";
        var email = "valid@example.com";

        // Act - No exception should be thrown
        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Assert - We reach here without exception
        Assert.Empty(errors);
    }

    #endregion
}

public class ValidationIntegrationTests
{
    #region Complex Validation Scenarios

    [Fact]
    public void ComplexValidation_MultipleFieldErrors_CreatesDetailedResult()
    {
        // Arrange
        var user = new TestUser
        {
            Username = "",
            Email = "invalid",
            Age = -5,
            Website = "not a url"
        };

        var errors = new List<ValidationError>();

        // Simulate validation logic
        if (string.IsNullOrWhiteSpace(user.Username))
            errors.Add(new ValidationError(nameof(user.Username), "Username is required", "USERNAME_REQUIRED"));

        if (!user.Email.Contains("@"))
            errors.Add(new ValidationError(nameof(user.Email), "Email must be valid", "EMAIL_INVALID", user.Email));

        if (user.Age < 0)
            errors.Add(new ValidationError(nameof(user.Age), "Age must be positive", "AGE_INVALID", user.Age));

        if (!user.Website.StartsWith("http"))
            errors.Add(new ValidationError(nameof(user.Website), "Website must be a valid URL", "URL_INVALID", user.Website));

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(user.Username));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(user.Email));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(user.Age));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(user.Website));
    }

    [Fact]
    public void ComplexValidation_PartialErrors_CreatesPartialResult()
    {
        // Arrange
        var user = new TestUser
        {
            Username = "validuser",
            Email = "invalid",
            Age = 25,
            Website = "https://example.com"
        };

        var errors = new List<ValidationError>();

        // Simulate validation logic
        if (string.IsNullOrWhiteSpace(user.Username))
            errors.Add(new ValidationError(nameof(user.Username), "Username is required"));

        if (!user.Email.Contains("@"))
            errors.Add(new ValidationError(nameof(user.Email), "Email must be valid", "EMAIL_INVALID", user.Email));

        if (user.Age < 0)
            errors.Add(new ValidationError(nameof(user.Age), "Age must be positive"));

        if (!user.Website.StartsWith("http"))
            errors.Add(new ValidationError(nameof(user.Website), "Website must be a valid URL"));

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(nameof(user.Email), result.Errors[0].PropertyName);
    }

    [Fact]
    public void ComplexValidation_NoErrors_CreatesSuccessResult()
    {
        // Arrange
        var user = new TestUser
        {
            Username = "validuser",
            Email = "valid@example.com",
            Age = 25,
            Website = "https://example.com"
        };

        var errors = new List<ValidationError>();

        // Simulate validation logic (all pass)
        if (string.IsNullOrWhiteSpace(user.Username))
            errors.Add(new ValidationError(nameof(user.Username), "Username is required"));

        if (!user.Email.Contains("@"))
            errors.Add(new ValidationError(nameof(user.Email), "Email must be valid"));

        if (user.Age < 0)
            errors.Add(new ValidationError(nameof(user.Age), "Age must be positive"));

        if (!user.Website.StartsWith("http"))
            errors.Add(new ValidationError(nameof(user.Website), "Website must be a valid URL"));

        // Act
        var result = errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion
}

// Test helper class
public class TestUser
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Website { get; set; } = string.Empty;
}
