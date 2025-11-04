namespace AppliedAccountability.RateLimiting;

/// <summary>
/// Service for managing rate limiting operations
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Attempts to acquire a permit for rate limiting
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit (e.g., user ID, IP address)</param>
    /// <param name="algorithm">Rate limiting algorithm to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating whether the request is allowed</returns>
    Task<RateLimitResult> TryAcquireAsync(
        string key,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.TokenBucket,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status for a rate limit key
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit</param>
    /// <param name="algorithm">Rate limiting algorithm to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current rate limit status</returns>
    Task<RateLimitStatus> GetStatusAsync(
        string key,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.TokenBucket,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for a specific key
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a rate limit check
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Number of remaining requests
    /// </summary>
    public int RemainingRequests { get; set; }

    /// <summary>
    /// Total request limit
    /// </summary>
    public int TotalLimit { get; set; }

    /// <summary>
    /// Time until the limit resets
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Timestamp when the limit will reset
    /// </summary>
    public DateTime? ResetAt { get; set; }

    public static RateLimitResult Allowed(int remaining, int total, DateTime? resetAt = null)
        => new()
        {
            IsAllowed = true,
            RemainingRequests = remaining,
            TotalLimit = total,
            ResetAt = resetAt
        };

    public static RateLimitResult Denied(int total, TimeSpan retryAfter, DateTime resetAt)
        => new()
        {
            IsAllowed = false,
            RemainingRequests = 0,
            TotalLimit = total,
            RetryAfter = retryAfter,
            ResetAt = resetAt
        };
}

/// <summary>
/// Current status of a rate limit
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Number of requests made
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Total request limit
    /// </summary>
    public int TotalLimit { get; set; }

    /// <summary>
    /// Number of remaining requests
    /// </summary>
    public int RemainingRequests => Math.Max(0, TotalLimit - RequestCount);

    /// <summary>
    /// Timestamp when the limit will reset
    /// </summary>
    public DateTime ResetAt { get; set; }

    /// <summary>
    /// Time until the limit resets
    /// </summary>
    public TimeSpan TimeUntilReset => ResetAt > DateTime.UtcNow
        ? ResetAt - DateTime.UtcNow
        : TimeSpan.Zero;
}

/// <summary>
/// Rate limiting algorithm types
/// </summary>
public enum RateLimitAlgorithm
{
    /// <summary>
    /// Token bucket algorithm - requests consume tokens that regenerate over time
    /// </summary>
    TokenBucket,

    /// <summary>
    /// Fixed window - resets at fixed intervals
    /// </summary>
    FixedWindow,

    /// <summary>
    /// Sliding window - tracks requests over a rolling time period
    /// </summary>
    SlidingWindow,

    /// <summary>
    /// Sliding window with logarithmic buckets for efficiency
    /// </summary>
    SlidingWindowLog
}
