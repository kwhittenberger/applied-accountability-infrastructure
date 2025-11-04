namespace AppliedAccountability.Infrastructure.Observability;

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
