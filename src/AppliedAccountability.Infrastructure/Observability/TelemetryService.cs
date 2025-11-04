using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.Infrastructure.Observability;

/// <summary>
/// Comprehensive telemetry service for metrics, tracing, and observability
/// Uses System.Diagnostics.Metrics and Activity for OpenTelemetry compatibility
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    // Metrics instruments
    private readonly Counter<long> _counter;
    private readonly Histogram<double> _histogram;
    private readonly ObservableGauge<double> _gauge;
    private readonly Dictionary<string, double> _gaugeValues;

    public TelemetryService(ILogger<TelemetryService> logger, string serviceName = "AppliedAccountability.Infrastructure")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize ActivitySource for distributed tracing
        _activitySource = new ActivitySource(serviceName, "1.0.0");

        // Initialize Meter for metrics
        _meter = new Meter(serviceName, "1.0.0");

        // Initialize metric instruments
        _counter = _meter.CreateCounter<long>("infrastructure.counter", description: "Counter for various infrastructure metrics");
        _histogram = _meter.CreateHistogram<double>("infrastructure.histogram", description: "Histogram for distribution metrics");

        _gaugeValues = new Dictionary<string, double>();
        _gauge = _meter.CreateObservableGauge<double>(
            "infrastructure.gauge",
            () => _gaugeValues.Select(kvp => new Measurement<double>(kvp.Value, new KeyValuePair<string, object?>("name", kvp.Key))),
            description: "Gauge for current state metrics");

        _logger.LogInformation("TelemetryService initialized for service: {ServiceName}", serviceName);
    }

    /// <inheritdoc />
    public Activity? StartActivity(string activityName, ActivityKind kind = ActivityKind.Internal)
    {
        var activity = _activitySource.StartActivity(activityName, kind);

        if (activity != null)
        {
            _logger.LogDebug(
                "Started activity: {ActivityName}, TraceId: {TraceId}, SpanId: {SpanId}",
                activityName,
                activity.TraceId,
                activity.SpanId);
        }

        return activity;
    }

    /// <inheritdoc />
    public void RecordCounter(string name, long value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var tagList = new TagList(tags);
            tagList.Add("metric_name", name);
            _counter.Add(value, tagList);

            _logger.LogDebug("Counter recorded: {Name} = {Value}, Tags: {Tags}", name, value, tags);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when recording counter: {Name}", name);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when recording counter: {Name}", name);
        }
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var key = $"{name}_{string.Join("_", tags.Select(t => $"{t.Key}={t.Value}"))}";
            _gaugeValues[key] = value;

            _logger.LogDebug("Gauge recorded: {Name} = {Value}, Tags: {Tags}", name, value, tags);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when recording gauge: {Name}", name);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when recording gauge: {Name}", name);
        }
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var tagList = new TagList(tags);
            tagList.Add("metric_name", name);
            _histogram.Record(value, tagList);

            _logger.LogDebug("Histogram recorded: {Name} = {Value}, Tags: {Tags}", name, value, tags);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when recording histogram: {Name}", name);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when recording histogram: {Name}", name);
        }
    }

    /// <inheritdoc />
    public void RecordException(Exception exception, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.RecordException(exception);
            }

            var tagList = tags.ToList();
            tagList.Add(new KeyValuePair<string, object?>("exception.type", exception.GetType().Name));
            tagList.Add(new KeyValuePair<string, object?>("exception.message", exception.Message));

            RecordCounter("exceptions", 1, tagList.ToArray());

            _logger.LogError(
                exception,
                "Exception recorded: {ExceptionType}, Message: {Message}, Tags: {Tags}",
                exception.GetType().Name,
                exception.Message,
                tags);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when recording exception");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when recording exception");
        }
    }

    /// <inheritdoc />
    public void RecordEvent(string eventName, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                var tagsCollection = new ActivityTagsCollection(tags);
                activity.AddEvent(new ActivityEvent(eventName, tags: tagsCollection));
            }

            RecordCounter("events", 1, tags.Concat(new[] { new KeyValuePair<string, object?>("event_name", eventName) }).ToArray());

            _logger.LogInformation("Event recorded: {EventName}, Tags: {Tags}", eventName, tags);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when recording event: {EventName}", eventName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when recording event: {EventName}", eventName);
        }
    }

    /// <inheritdoc />
    public void AddTag(string key, object? value)
    {
        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag(key, value);
                _logger.LogDebug("Tag added to activity: {Key} = {Value}", key, value);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when adding tag: {Key}", key);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when adding tag: {Key}", key);
        }
    }

    /// <inheritdoc />
    public void AddBaggage(string key, string? value)
    {
        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetBaggage(key, value);
                _logger.LogDebug("Baggage added to activity: {Key} = {Value}", key, value);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when adding baggage: {Key}", key);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when adding baggage: {Key}", key);
        }
    }
}
