namespace AppliedAccountability.EventStore.Models;

/// <summary>
/// Represents a point-in-time snapshot of aggregate state.
/// Used for performance optimization to avoid replaying all events.
/// </summary>
public class Snapshot
{
    /// <summary>
    /// Auto-incrementing snapshot identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Stream identifier (aggregate ID).
    /// </summary>
    public required string StreamId { get; set; }

    /// <summary>
    /// Last event version included in this snapshot.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Serialized aggregate state as JSON string.
    /// </summary>
    public required string StateData { get; set; }

    /// <summary>
    /// Timestamp when the snapshot was created (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }
}
