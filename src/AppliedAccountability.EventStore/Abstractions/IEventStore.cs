using AppliedAccountability.EventStore.Models;

namespace AppliedAccountability.EventStore.Abstractions;

/// <summary>
/// Interface for event store operations.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends multiple events to a stream.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="events">Events to append.</param>
    /// <param name="expectedVersion">Expected current version for optimistic concurrency control. Null to skip check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.ConcurrencyException">Thrown when expected version doesn't match actual version.</exception>
    Task AppendAsync(
        string streamId,
        IEnumerable<EventData> events,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a single event to a stream (convenience method).
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="eventData">Event to append.</param>
    /// <param name="expectedVersion">Expected current version for optimistic concurrency control. Null to skip check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.ConcurrencyException">Thrown when expected version doesn't match actual version.</exception>
    Task AppendAsync(
        string streamId,
        EventData eventData,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all events from a stream.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="fromVersion">Start reading from this version (inclusive, default: 1).</param>
    /// <param name="toVersion">Stop reading at this version (inclusive, default: latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of events in chronological order.</returns>
    Task<IReadOnlyList<Event>> ReadStreamAsync(
        string streamId,
        int fromVersion = 1,
        int? toVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events forward (chronological order) from a global position.
    /// </summary>
    /// <param name="fromPosition">Start reading from this position (default: 0 for beginning).</param>
    /// <param name="maxCount">Maximum number of events to return (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of events in chronological order.</returns>
    Task<IReadOnlyList<Event>> ReadForwardAsync(
        long fromPosition = 0,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events by event type.
    /// </summary>
    /// <param name="eventType">Event type to filter by.</param>
    /// <param name="fromTimestamp">Start reading from this timestamp (inclusive, default: earliest).</param>
    /// <param name="toTimestamp">Stop reading at this timestamp (inclusive, default: latest).</param>
    /// <param name="maxCount">Maximum number of events to return (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of events matching the event type.</returns>
    Task<IReadOnlyList<Event>> ReadEventTypeAsync(
        string eventType,
        DateTime? fromTimestamp = null,
        DateTime? toTimestamp = null,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all events with a specific correlation ID.
    /// </summary>
    /// <param name="correlationId">Correlation ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of events with the correlation ID.</returns>
    Task<IReadOnlyList<Event>> ReadByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a stream.
    /// </summary>
    /// <param name="streamId">Stream identifier (aggregate ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current version, or 0 if stream doesn't exist.</returns>
    Task<int> GetStreamVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default);
}
