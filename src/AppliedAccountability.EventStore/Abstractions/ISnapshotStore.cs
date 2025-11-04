using AppliedAccountability.EventStore.Models;

namespace AppliedAccountability.EventStore.Abstractions;

/// <summary>
/// Interface for snapshot store operations.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves a snapshot for a stream at a specific version.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="version">Version of the last event included in the snapshot.</param>
    /// <param name="state">Aggregate state object (will be serialized to JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSnapshotAsync(
        string streamId,
        int version,
        object state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot for a stream.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest snapshot, or null if no snapshots exist.</returns>
    Task<Snapshot?> GetLatestSnapshotAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific snapshot by version.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="version">Snapshot version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Snapshot at the specified version, or null if not found.</returns>
    Task<Snapshot?> GetSnapshotAsync(
        string streamId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old snapshots for a stream, keeping only the latest N snapshots.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="keepCount">Number of snapshots to keep (default: 3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOldSnapshotsAsync(
        string streamId,
        int keepCount = 3,
        CancellationToken cancellationToken = default);
}
