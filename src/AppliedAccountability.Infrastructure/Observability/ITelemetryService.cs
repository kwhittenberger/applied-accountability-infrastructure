using System.Diagnostics;

namespace AppliedAccountability.Infrastructure.Observability;

/// <summary>
/// Service for comprehensive telemetry, metrics, and distributed tracing
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Starts a new activity (span) for distributed tracing
    /// </summary>
    Activity? StartActivity(string activityName, ActivityKind kind = ActivityKind.Internal);

    /// <summary>
    /// Records a counter metric (incremental value)
    /// </summary>
    void RecordCounter(string name, long value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a gauge metric (current value)
    /// </summary>
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a histogram metric (distribution of values)
    /// </summary>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records an exception for telemetry
    /// </summary>
    void RecordException(Exception exception, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a custom event
    /// </summary>
    void RecordEvent(string eventName, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Adds a tag/attribute to the current activity
    /// </summary>
    void AddTag(string key, object? value);

    /// <summary>
    /// Adds baggage (context propagation) to the current activity
    /// </summary>
    void AddBaggage(string key, string? value);
}

/// <summary>
/// Extension methods for telemetry operations
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Executes an action within a traced activity
    /// </summary>
    public static void ExecuteWithTracing(
        this ITelemetryService telemetry,
        string activityName,
        Action action,
        ActivityKind kind = ActivityKind.Internal)
    {
        using var activity = telemetry.StartActivity(activityName, kind);
        try
        {
            action();
        }
        catch (Exception ex)
        {
            telemetry.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes an async action within a traced activity
    /// </summary>
    public static async Task ExecuteWithTracingAsync(
        this ITelemetryService telemetry,
        string activityName,
        Func<Task> action,
        ActivityKind kind = ActivityKind.Internal)
    {
        using var activity = telemetry.StartActivity(activityName, kind);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            telemetry.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a function within a traced activity and returns result
    /// </summary>
    public static async Task<T> ExecuteWithTracingAsync<T>(
        this ITelemetryService telemetry,
        string activityName,
        Func<Task<T>> func,
        ActivityKind kind = ActivityKind.Internal)
    {
        using var activity = telemetry.StartActivity(activityName, kind);
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            telemetry.RecordException(ex);
            throw;
        }
    }
}
