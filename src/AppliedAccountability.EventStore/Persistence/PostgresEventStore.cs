using System.Text.Json;
using AppliedAccountability.EventStore.Abstractions;
using AppliedAccountability.EventStore.Exceptions;
using AppliedAccountability.EventStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.EventStore.Persistence;

/// <summary>
/// PostgreSQL implementation of IEventStore.
/// </summary>
public class PostgresEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly ILogger<PostgresEventStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostgresEventStore(
        EventStoreDbContext context,
        ILogger<PostgresEventStore> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task AppendAsync(
        string streamId,
        IEnumerable<EventData> events,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any())
        {
            _logger.LogWarning("Attempted to append zero events to stream {StreamId}", streamId);
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get current version
            var currentVersion = await GetStreamVersionAsync(streamId, cancellationToken);

            // Check expected version if provided
            if (expectedVersion.HasValue && currentVersion != expectedVersion.Value)
            {
                throw new ConcurrencyException(streamId, expectedVersion.Value, currentVersion);
            }

            // Append events
            var nextVersion = currentVersion + 1;
            foreach (var eventData in eventsList)
            {
                var @event = new Event
                {
                    StreamId = streamId,
                    EventType = eventData.EventType,
                    EventData = JsonSerializer.Serialize(eventData.Data, _jsonOptions),
                    Metadata = eventData.Metadata != null
                        ? JsonSerializer.Serialize(eventData.Metadata, _jsonOptions)
                        : null,
                    Version = nextVersion++,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = eventData.CorrelationId
                };

                _context.Events.Add(@event);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Appended {EventCount} event(s) to stream {StreamId} (versions {StartVersion} to {EndVersion})",
                eventsList.Count,
                streamId,
                currentVersion + 1,
                nextVersion - 1);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true)
        {
            // Concurrency conflict detected
            await transaction.RollbackAsync(cancellationToken);
            var actualVersion = await GetStreamVersionAsync(streamId, cancellationToken);
            throw new ConcurrencyException(streamId, expectedVersion ?? currentVersion, actualVersion, ex);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task AppendAsync(
        string streamId,
        EventData eventData,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        return AppendAsync(streamId, new[] { eventData }, expectedVersion, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> ReadStreamAsync(
        string streamId,
        int fromVersion = 1,
        int? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Where(e => e.StreamId == streamId && e.Version >= fromVersion)
            .AsNoTracking();

        if (toVersion.HasValue)
        {
            query = query.Where(e => e.Version <= toVersion.Value);
        }

        var events = await query
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Read {EventCount} event(s) from stream {StreamId} (versions {FromVersion} to {ToVersion})",
            events.Count,
            streamId,
            fromVersion,
            toVersion?.ToString() ?? "latest");

        return events;
    }

    public async Task<IReadOnlyList<Event>> ReadForwardAsync(
        long fromPosition = 0,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .Where(e => e.Id > fromPosition)
            .OrderBy(e => e.Id)
            .Take(maxCount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Read {EventCount} event(s) forward from position {FromPosition}",
            events.Count,
            fromPosition);

        return events;
    }

    public async Task<IReadOnlyList<Event>> ReadEventTypeAsync(
        string eventType,
        DateTime? fromTimestamp = null,
        DateTime? toTimestamp = null,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Where(e => e.EventType == eventType)
            .AsNoTracking();

        if (fromTimestamp.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromTimestamp.Value);
        }

        if (toTimestamp.HasValue)
        {
            query = query.Where(e => e.Timestamp <= toTimestamp.Value);
        }

        var events = await query
            .OrderBy(e => e.Timestamp)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Read {EventCount} event(s) of type {EventType}",
            events.Count,
            eventType);

        return events;
    }

    public async Task<IReadOnlyList<Event>> ReadByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Read {EventCount} event(s) with correlation ID {CorrelationId}",
            events.Count,
            correlationId);

        return events;
    }

    public async Task<int> GetStreamVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        var version = await _context.Events
            .Where(e => e.StreamId == streamId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return version ?? 0;
    }
}
