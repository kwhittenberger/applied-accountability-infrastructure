namespace AppliedAccountability.Observability;

/// <summary>
/// Health check result for infrastructure components
/// </summary>
public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }

    public static HealthCheckResult Healthy(string description = "Healthy", Dictionary<string, object>? data = null)
        => new()
        {
            Status = HealthStatus.Healthy,
            Description = description,
            Data = data ?? new Dictionary<string, object>()
        };

    public static HealthCheckResult Degraded(string description, Dictionary<string, object>? data = null)
        => new()
        {
            Status = HealthStatus.Degraded,
            Description = description,
            Data = data ?? new Dictionary<string, object>()
        };

    public static HealthCheckResult Unhealthy(string description, Exception? exception = null, Dictionary<string, object>? data = null)
        => new()
        {
            Status = HealthStatus.Unhealthy,
            Description = description,
            Exception = exception,
            Data = data ?? new Dictionary<string, object>()
        };
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Interface for custom health checks
/// </summary>
public interface IHealthCheck
{
    string Name { get; }
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated health check result for the entire application
/// </summary>
public class ApplicationHealthResult
{
    public HealthStatus OverallStatus { get; set; }
    public Dictionary<string, HealthCheckResult> ComponentResults { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApplicationHealthResult FromComponents(Dictionary<string, HealthCheckResult> results, TimeSpan duration)
    {
        var overallStatus = HealthStatus.Healthy;

        if (results.Values.Any(r => r.Status == HealthStatus.Unhealthy))
            overallStatus = HealthStatus.Unhealthy;
        else if (results.Values.Any(r => r.Status == HealthStatus.Degraded))
            overallStatus = HealthStatus.Degraded;

        return new ApplicationHealthResult
        {
            OverallStatus = overallStatus,
            ComponentResults = results,
            TotalDuration = duration
        };
    }
}

/// <summary>
/// Service for executing health checks
/// </summary>
public interface IHealthCheckService
{
    Task<ApplicationHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);
    void RegisterHealthCheck(IHealthCheck healthCheck);
}

/// <summary>
/// Default implementation of health check service
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly Dictionary<string, IHealthCheck> _healthChecks = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void RegisterHealthCheck(IHealthCheck healthCheck)
    {
        ArgumentNullException.ThrowIfNull(healthCheck);

        lock (_lock)
        {
            _healthChecks[healthCheck.Name] = healthCheck;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var results = new Dictionary<string, HealthCheckResult>();

        IHealthCheck[] checks;
        lock (_lock)
        {
            checks = _healthChecks.Values.ToArray();
        }

        foreach (var check in checks)
        {
            var checkStartTime = DateTime.UtcNow;
            try
            {
                var result = await check.CheckHealthAsync(cancellationToken);
                result.Duration = DateTime.UtcNow - checkStartTime;
                results[check.Name] = result;
            }
            catch (Exception ex)
            {
                results[check.Name] = HealthCheckResult.Unhealthy(
                    $"Health check failed: {ex.Message}",
                    ex,
                    new Dictionary<string, object> { ["duration"] = (DateTime.UtcNow - checkStartTime).TotalMilliseconds }
                );
            }
        }

        var totalDuration = DateTime.UtcNow - startTime;
        return ApplicationHealthResult.FromComponents(results, totalDuration);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default)
    {
        IHealthCheck? check;
        lock (_lock)
        {
            _healthChecks.TryGetValue(componentName, out check);
        }

        if (check == null)
        {
            return HealthCheckResult.Unhealthy($"Health check '{componentName}' not found");
        }

        var startTime = DateTime.UtcNow;
        try
        {
            var result = await check.CheckHealthAsync(cancellationToken);
            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                ex,
                new Dictionary<string, object> { ["duration"] = (DateTime.UtcNow - startTime).TotalMilliseconds }
            );
        }
    }
}
