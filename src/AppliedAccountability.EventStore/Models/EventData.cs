namespace AppliedAccountability.EventStore.Models;

/// <summary>
/// Represents event data to be appended to the event store.
/// </summary>
public class EventData
{
    /// <summary>
    /// Event type identifier.
    /// Example: "UserCreated", "OrderPlaced"
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Event payload object (will be serialized to JSON).
    /// </summary>
    public required object Data { get; set; }

    /// <summary>
    /// Optional metadata object (will be serialized to JSON).
    /// Can include: user ID, IP address, request ID, etc.
    /// </summary>
    public object? Metadata { get; set; }

    /// <summary>
    /// Correlation ID for tracing related events.
    /// Automatically generated if not provided.
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
