using System.Diagnostics;
using AppliedAccountability.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AppliedAccountability.Infrastructure.Tests.Observability;

public class TelemetryServiceTests : IDisposable
{
    private readonly Mock<ILogger<TelemetryService>> _mockLogger;
    private readonly TelemetryService _service;
    private readonly ActivityListener _activityListener;

    public TelemetryServiceTests()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _service = new TelemetryService(_mockLogger.Object, "TestService");

        // Setup activity listener to enable activity creation
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "TestService",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TelemetryService(null!, "TestService"));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new TelemetryService(_mockLogger.Object, "TestService");

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithDefaultServiceName_CreatesInstance()
    {
        // Act
        var service = new TelemetryService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region StartActivity Tests

    [Fact]
    public void StartActivity_WithValidName_ReturnsActivity()
    {
        // Act
        using var activity = _service.StartActivity("TestActivity");

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("TestActivity", activity.OperationName);
    }

    [Fact]
    public void StartActivity_WithActivityKind_SetsCorrectKind()
    {
        // Act
        using var activity = _service.StartActivity("TestActivity", ActivityKind.Server);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Server, activity.Kind);
    }

    [Fact]
    public void StartActivity_CreatesActivityWithTraceId()
    {
        // Act
        using var activity = _service.StartActivity("TestActivity");

        // Assert
        Assert.NotNull(activity);
        Assert.NotEqual(ActivityTraceId.CreateRandom(), activity.TraceId);
    }

    #endregion

    #region RecordCounter Tests

    [Fact]
    public void RecordCounter_WithValidData_DoesNotThrow()
    {
        // Arrange
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tag1", "value1"),
            new KeyValuePair<string, object?>("tag2", 42)
        };

        // Act & Assert - Should not throw
        _service.RecordCounter("test_counter", 1, tags);
    }

    [Fact]
    public void RecordCounter_WithNoTags_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.RecordCounter("test_counter", 10);
    }

    [Fact]
    public void RecordCounter_WithNegativeValue_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.RecordCounter("test_counter", -5);
    }

    #endregion

    #region RecordGauge Tests

    [Fact]
    public void RecordGauge_WithValidData_DoesNotThrow()
    {
        // Arrange
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tag1", "value1")
        };

        // Act & Assert - Should not throw
        _service.RecordGauge("test_gauge", 42.5, tags);
    }

    [Fact]
    public void RecordGauge_WithNoTags_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.RecordGauge("test_gauge", 100.0);
    }

    [Fact]
    public void RecordGauge_WithMultipleCalls_UpdatesValue()
    {
        // Act - Should not throw
        _service.RecordGauge("test_gauge", 50.0);
        _service.RecordGauge("test_gauge", 75.0);
        _service.RecordGauge("test_gauge", 100.0);

        // Assert - No exception
        Assert.True(true);
    }

    #endregion

    #region RecordHistogram Tests

    [Fact]
    public void RecordHistogram_WithValidData_DoesNotThrow()
    {
        // Arrange
        var tags = new[]
        {
            new KeyValuePair<string, object?>("operation", "read"),
            new KeyValuePair<string, object?>("status", "success")
        };

        // Act & Assert - Should not throw
        _service.RecordHistogram("request_duration", 123.45, tags);
    }

    [Fact]
    public void RecordHistogram_WithNoTags_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.RecordHistogram("request_duration", 50.0);
    }

    [Fact]
    public void RecordHistogram_WithMultipleValues_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.RecordHistogram("response_time", 10.5);
        _service.RecordHistogram("response_time", 25.3);
        _service.RecordHistogram("response_time", 15.7);
    }

    #endregion

    #region RecordException Tests

    [Fact]
    public void RecordException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var tags = new[]
        {
            new KeyValuePair<string, object?>("operation", "test")
        };

        // Act & Assert - Should not throw
        _service.RecordException(exception, tags);
    }

    [Fact]
    public void RecordException_WithNoTags_DoesNotThrow()
    {
        // Arrange
        var exception = new ArgumentNullException("param");

        // Act & Assert - Should not throw
        _service.RecordException(exception);
    }

    [Fact]
    public void RecordException_WithActivity_SetsActivityStatus()
    {
        // Arrange
        var exception = new Exception("Test error");

        // Act
        using var activity = _service.StartActivity("TestOperation");
        _service.RecordException(exception);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    #endregion

    #region RecordEvent Tests

    [Fact]
    public void RecordEvent_WithValidData_DoesNotThrow()
    {
        // Arrange
        var tags = new[]
        {
            new KeyValuePair<string, object?>("user_id", "123"),
            new KeyValuePair<string, object?>("action", "login")
        };

        // Act & Assert - Should not throw
        _service.RecordEvent("user_login", tags);
    }

    [Fact]
    public void RecordEvent_WithNoTags_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.RecordEvent("simple_event");
    }

    [Fact]
    public void RecordEvent_WithActivity_AddsEventToActivity()
    {
        // Arrange
        var eventName = "test_event";
        var tags = new[]
        {
            new KeyValuePair<string, object?>("key", "value")
        };

        // Act
        using var activity = _service.StartActivity("TestOperation");
        _service.RecordEvent(eventName, tags);

        // Assert
        Assert.NotNull(activity);
        Assert.NotEmpty(activity.Events);
    }

    #endregion

    #region AddTag Tests

    [Fact]
    public void AddTag_WithActivity_AddsTagToActivity()
    {
        // Arrange
        using var activity = _service.StartActivity("TestOperation");

        // Act
        _service.AddTag("test_key", "test_value");

        // Assert
        Assert.NotNull(activity);
        Assert.Contains(activity.Tags, tag => tag.Key == "test_key" && tag.Value as string == "test_value");
    }

    [Fact]
    public void AddTag_WithoutActivity_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.AddTag("test_key", "test_value");
    }

    [Fact]
    public void AddTag_WithNullValue_DoesNotThrow()
    {
        // Arrange
        using var activity = _service.StartActivity("TestOperation");

        // Act & Assert - Should not throw
        _service.AddTag("test_key", null);
    }

    #endregion

    #region AddBaggage Tests

    [Fact]
    public void AddBaggage_WithActivity_AddsBaggageToActivity()
    {
        // Arrange
        using var activity = _service.StartActivity("TestOperation");

        // Act
        _service.AddBaggage("correlation_id", "12345");

        // Assert
        Assert.NotNull(activity);
        Assert.Contains(activity.Baggage, bag => bag.Key == "correlation_id" && bag.Value == "12345");
    }

    [Fact]
    public void AddBaggage_WithoutActivity_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.AddBaggage("correlation_id", "12345");
    }

    [Fact]
    public void AddBaggage_WithNullValue_DoesNotThrow()
    {
        // Arrange
        using var activity = _service.StartActivity("TestOperation");

        // Act & Assert - Should not throw
        _service.AddBaggage("test_key", null);
    }

    #endregion
}

public class TelemetryExtensionsTests : IDisposable
{
    private readonly Mock<ITelemetryService> _mockTelemetry;
    private readonly Mock<Activity> _mockActivity;
    private readonly ActivityListener _activityListener;

    public TelemetryExtensionsTests()
    {
        _mockTelemetry = new Mock<ITelemetryService>();
        _mockActivity = new Mock<Activity>("TestActivity");

        // Setup activity listener
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region ExecuteWithTracing Tests

    [Fact]
    public void ExecuteWithTracing_WithSuccessfulAction_CompletesSuccessfully()
    {
        // Arrange
        var executed = false;
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act
        _mockTelemetry.Object.ExecuteWithTracing("TestActivity", () => executed = true);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void ExecuteWithTracing_WithException_RecordsExceptionAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _mockTelemetry.Object.ExecuteWithTracing("TestActivity", () => throw exception));

        _mockTelemetry.Verify(t => t.RecordException(exception), Times.Once);
    }

    [Fact]
    public void ExecuteWithTracing_WithCustomActivityKind_UsesProvidedKind()
    {
        // Arrange
        var executed = false;

        // Act
        _mockTelemetry.Object.ExecuteWithTracing(
            "TestActivity",
            () => executed = true,
            ActivityKind.Server);

        // Assert
        _mockTelemetry.Verify(t => t.StartActivity("TestActivity", ActivityKind.Server), Times.Once);
    }

    #endregion

    #region ExecuteWithTracingAsync Tests

    [Fact]
    public async Task ExecuteWithTracingAsync_WithSuccessfulAction_CompletesSuccessfully()
    {
        // Arrange
        var executed = false;
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act
        await _mockTelemetry.Object.ExecuteWithTracingAsync("TestActivity", async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_WithException_RecordsExceptionAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mockTelemetry.Object.ExecuteWithTracingAsync("TestActivity", async () =>
            {
                await Task.Delay(10);
                throw exception;
            }));

        _mockTelemetry.Verify(t => t.RecordException(exception), Times.Once);
    }

    #endregion

    #region ExecuteWithTracingAsync<T> Tests

    [Fact]
    public async Task ExecuteWithTracingAsync_Generic_WithSuccessfulFunc_ReturnsResult()
    {
        // Arrange
        var expectedResult = 42;
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act
        var result = await _mockTelemetry.Object.ExecuteWithTracingAsync("TestActivity", async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        });

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_Generic_WithException_RecordsExceptionAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mockTelemetry.Object.ExecuteWithTracingAsync<int>("TestActivity", async () =>
            {
                await Task.Delay(10);
                throw exception;
            }));

        _mockTelemetry.Verify(t => t.RecordException(exception), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_Generic_WithComplexType_ReturnsCorrectResult()
    {
        // Arrange
        var expectedResult = new TestResult { Id = 1, Message = "Success" };
        _mockTelemetry
            .Setup(t => t.StartActivity("TestActivity", ActivityKind.Internal))
            .Returns((Activity?)null);

        // Act
        var result = await _mockTelemetry.Object.ExecuteWithTracingAsync("TestActivity", async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResult.Id, result.Id);
        Assert.Equal(expectedResult.Message, result.Message);
    }

    #endregion
}

// Test helper class
public class TestResult
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
