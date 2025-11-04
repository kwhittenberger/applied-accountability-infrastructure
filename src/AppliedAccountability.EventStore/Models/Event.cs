namespace AppliedAccountability.EventStore.Models;

/// <summary>
/// Represents an immutable event in the event store.
/// </summary>
public class Event
{
    /// <summary>
    /// Auto-incrementing event identifier (global position in event store).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Stream identifier (aggregate ID). All events for the same aggregate share this ID.
    /// Example: "user-123", "order-456"
    /// </summary>
    public required string StreamId { get; set; }

    /// <summary>
    /// Event type identifier.
    /// Example: "UserCreated", "OrderPlaced", "PaymentProcessed"
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Event payload as JSON string.
    /// </summary>
    public required string EventData { get; set; }

    /// <summary>
    /// Optional metadata as JSON string.
    /// Can include: user ID, IP address, correlation IDs, etc.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Event version within the stream (starts at 1).
    /// Used for optimistic concurrency control.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Timestamp when the event occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Correlation ID for tracing related events across streams.
    /// </summary>
    public Guid CorrelationId { get; set; }
}
