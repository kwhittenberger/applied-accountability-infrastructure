using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.RateLimiting;

/// <summary>
/// Default implementation of rate limiting service
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly ILogger<RateLimitService> _logger;
    private readonly RateLimitOptions _options;
    private readonly Dictionary<string, RateLimiter> _limiters = new();
    private readonly object _lock = new();

    public RateLimitService(
        ILogger<RateLimitService> logger,
        RateLimitOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new RateLimitOptions();
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> TryAcquireAsync(
        string key,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.TokenBucket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var limiter = GetOrCreateLimiter(key, algorithm);

        try
        {
            var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken);

            if (lease.IsAcquired)
            {
                var metadata = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter);
                var resetAt = metadata && retryAfter.HasValue
                    ? DateTime.UtcNow.Add(retryAfter.Value)
                    : DateTime.UtcNow.Add(_options.Window);

                _logger.LogDebug(
                    "Rate limit acquired for key: {Key}, Algorithm: {Algorithm}",
                    key, algorithm);

                lease.Dispose();

                return RateLimitResult.Allowed(
                    remaining: _options.PermitLimit - 1,
                    total: _options.PermitLimit,
                    resetAt: resetAt);
            }
            else
            {
                var metadata = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter);
                var retryAfterValue = retryAfter ?? _options.Window;
                var resetAt = DateTime.UtcNow.Add(retryAfterValue);

                _logger.LogWarning(
                    "Rate limit exceeded for key: {Key}, Algorithm: {Algorithm}, RetryAfter: {RetryAfter}s",
                    key, algorithm, retryAfterValue.TotalSeconds);

                lease.Dispose();

                return RateLimitResult.Denied(
                    total: _options.PermitLimit,
                    retryAfter: retryAfterValue,
                    resetAt: resetAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring rate limit for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<RateLimitStatus> GetStatusAsync(
        string key,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.TokenBucket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Note: System.Threading.RateLimiting doesn't provide direct access to current stats
        // In a production implementation, you'd track this separately in a cache
        var status = new RateLimitStatus
        {
            RequestCount = 0,
            TotalLimit = _options.PermitLimit,
            ResetAt = DateTime.UtcNow.Add(_options.Window)
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            if (_limiters.TryGetValue(key, out var limiter))
            {
                limiter.Dispose();
                _limiters.Remove(key);

                _logger.LogInformation("Rate limit reset for key: {Key}", key);
            }
        }

        return Task.CompletedTask;
    }

    private RateLimiter GetOrCreateLimiter(string key, RateLimitAlgorithm algorithm)
    {
        lock (_lock)
        {
            if (_limiters.TryGetValue(key, out var limiter))
            {
                return limiter;
            }

            limiter = CreateLimiter(algorithm);
            _limiters[key] = limiter;

            _logger.LogDebug(
                "Created new rate limiter for key: {Key}, Algorithm: {Algorithm}",
                key, algorithm);

            return limiter;
        }
    }

    private RateLimiter CreateLimiter(RateLimitAlgorithm algorithm)
    {
        return algorithm switch
        {
            RateLimitAlgorithm.TokenBucket => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = _options.PermitLimit,
                TokensPerPeriod = _options.PermitLimit,
                ReplenishmentPeriod = _options.Window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }),

            RateLimitAlgorithm.FixedWindow => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = _options.PermitLimit,
                Window = _options.Window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }),

            RateLimitAlgorithm.SlidingWindow => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = _options.PermitLimit,
                Window = _options.Window,
                SegmentsPerWindow = 8,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }),

            RateLimitAlgorithm.SlidingWindowLog => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = _options.PermitLimit,
                Window = _options.Window,
                SegmentsPerWindow = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }),

            _ => throw new ArgumentException($"Unsupported rate limit algorithm: {algorithm}", nameof(algorithm))
        };
    }
}

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Number of permits/requests allowed
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Default algorithm to use
    /// </summary>
    public RateLimitAlgorithm DefaultAlgorithm { get; set; } = RateLimitAlgorithm.TokenBucket;

    /// <summary>
    /// Whether to enable queueing when rate limit is exceeded
    /// </summary>
    public bool EnableQueueing { get; set; } = false;

    /// <summary>
    /// Queue size limit (0 = no queueing)
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}
