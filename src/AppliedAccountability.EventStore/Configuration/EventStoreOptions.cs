namespace AppliedAccountability.EventStore.Configuration;

/// <summary>
/// Configuration options for the event store.
/// </summary>
public class EventStoreOptions
{
    /// <summary>
    /// PostgreSQL connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot interval - create a snapshot every N events.
    /// Set to 0 to disable automatic snapshots.
    /// Default: 10.
    /// </summary>
    public int SnapshotInterval { get; set; } = 10;

    /// <summary>
    /// Number of old snapshots to keep per stream.
    /// Default: 3.
    /// </summary>
    public int SnapshotsToKeep { get; set; } = 3;

    /// <summary>
    /// Enable detailed logging for debugging.
    /// Default: false.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
