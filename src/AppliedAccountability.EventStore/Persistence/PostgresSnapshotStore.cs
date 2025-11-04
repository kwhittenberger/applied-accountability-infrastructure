using System.Text.Json;
using AppliedAccountability.EventStore.Abstractions;
using AppliedAccountability.EventStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.EventStore.Persistence;

/// <summary>
/// PostgreSQL implementation of ISnapshotStore.
/// </summary>
public class PostgresSnapshotStore : ISnapshotStore
{
    private readonly EventStoreDbContext _context;
    private readonly ILogger<PostgresSnapshotStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostgresSnapshotStore(
        EventStoreDbContext context,
        ILogger<PostgresSnapshotStore> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task SaveSnapshotAsync(
        string streamId,
        int version,
        object state,
        CancellationToken cancellationToken = default)
    {
        var snapshot = new Snapshot
        {
            StreamId = streamId,
            Version = version,
            StateData = JsonSerializer.Serialize(state, _jsonOptions),
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Saved snapshot for stream {StreamId} at version {Version}",
            streamId,
            version);
    }

    public async Task<Snapshot?> GetLatestSnapshotAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.Snapshots
            .Where(s => s.StreamId == streamId)
            .OrderByDescending(s => s.Version)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot != null)
        {
            _logger.LogDebug(
                "Retrieved latest snapshot for stream {StreamId} at version {Version}",
                streamId,
                snapshot.Version);
        }
        else
        {
            _logger.LogDebug(
                "No snapshots found for stream {StreamId}",
                streamId);
        }

        return snapshot;
    }

    public async Task<Snapshot?> GetSnapshotAsync(
        string streamId,
        int version,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.Snapshots
            .Where(s => s.StreamId == streamId && s.Version == version)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot != null)
        {
            _logger.LogDebug(
                "Retrieved snapshot for stream {StreamId} at version {Version}",
                streamId,
                version);
        }
        else
        {
            _logger.LogDebug(
                "No snapshot found for stream {StreamId} at version {Version}",
                streamId,
                version);
        }

        return snapshot;
    }

    public async Task DeleteOldSnapshotsAsync(
        string streamId,
        int keepCount = 3,
        CancellationToken cancellationToken = default)
    {
        var snapshotsToKeep = await _context.Snapshots
            .Where(s => s.StreamId == streamId)
            .OrderByDescending(s => s.Version)
            .Take(keepCount)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var snapshotsToDelete = await _context.Snapshots
            .Where(s => s.StreamId == streamId && !snapshotsToKeep.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (snapshotsToDelete.Any())
        {
            _context.Snapshots.RemoveRange(snapshotsToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Deleted {DeleteCount} old snapshot(s) for stream {StreamId}, keeping latest {KeepCount}",
                snapshotsToDelete.Count,
                streamId,
                keepCount);
        }
    }
}
