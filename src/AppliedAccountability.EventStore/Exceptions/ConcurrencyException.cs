namespace AppliedAccountability.EventStore.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict is detected during event append.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Stream identifier where the conflict occurred.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Expected version that was provided.
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// Actual current version in the stream.
    /// </summary>
    public int ActualVersion { get; }

    public ConcurrencyException(string streamId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict on stream '{streamId}'. Expected version {expectedVersion}, but actual version is {actualVersion}.")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public ConcurrencyException(string streamId, int expectedVersion, int actualVersion, Exception innerException)
        : base($"Concurrency conflict on stream '{streamId}'. Expected version {expectedVersion}, but actual version is {actualVersion}.", innerException)
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
