using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.Infrastructure.Caching;

/// <summary>
/// Distributed cache service with Redis and memory cache support
/// Provides automatic serialization/deserialization and comprehensive logging
/// </summary>
public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeSpan _defaultExpiration;

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger,
        TimeSpan? defaultExpiration = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for cache key: {CacheKey}", key);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Cache retrieval cancelled for key: {CacheKey}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };

            await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
            _logger.LogDebug(
                "Cache set for key: {CacheKey}, Expiration: {Expiration}",
                key,
                expiration ?? _defaultExpiration);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error for cache key: {CacheKey}", key);
            throw new CacheException($"Failed to serialize value for key: {key}", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Cache set operation cancelled for key: {CacheKey}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache removed for key: {CacheKey}", key);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Cache removal cancelled for key: {CacheKey}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(cachedData);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Cache existence check was cancelled for key: {CacheKey}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);

        var result = new Dictionary<string, T?>();

        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        _logger.LogDebug("Retrieved {Count} cache values", result.Count);
        return result;
    }

    /// <inheritdoc />
    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, expiration, cancellationToken);
        }

        _logger.LogDebug("Set {Count} cache values", items.Count);
    }

    /// <inheritdoc />
    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            await RemoveAsync(key, cancellationToken);
        }

        _logger.LogDebug("Removed {Count} cache values", keys.Count());
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _cache.RefreshAsync(key, cancellationToken);
            _logger.LogDebug("Cache refreshed for key: {CacheKey}", key);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Cache refresh cancelled for key: {CacheKey}", key);
            throw;
        }
    }
}

/// <summary>
/// Exception thrown by cache operations
/// </summary>
public class CacheException : Exception
{
    public CacheException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
