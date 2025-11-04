using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace AppliedAccountability.Api;

/// <summary>
/// Base API client with Polly resilience policies, common behaviors, and comprehensive logging.
/// All external API clients should inherit from this class to ensure consistent behavior.
/// </summary>
public abstract class BaseApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly JsonSerializerOptions JsonOptions;

    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    protected BaseApiClient(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _resiliencePolicy = BuildResiliencePolicy();
    }

    /// <summary>
    /// Makes an HTTP GET request with resilience policies and logging
    /// </summary>
    protected async Task<TResponse> GetAsync<TResponse>(
        string url,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        Logger.LogInformation(
            "[{RequestId}] Starting {OperationName} GET request to {Url}",
            requestId, operationName, url);

        try
        {
            using var response = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                var httpResponse = await HttpClient.GetAsync(url, cancellationToken);
                await EnsureSuccessStatusCodeAsync(httpResponse, url, requestId);
                return httpResponse;
            });

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = DeserializeResponse<TResponse>(content, url, requestId);

            stopwatch.Stop();

            Logger.LogInformation(
                "[{RequestId}] {OperationName} completed successfully in {ElapsedMs}ms. Response size: {ResponseSize} bytes",
                requestId, operationName, stopwatch.ElapsedMilliseconds, content.Length);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} HTTP request failed after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} timed out after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (ApiClientException)
        {
            stopwatch.Stop();
            throw;
        }
    }

    /// <summary>
    /// Makes an HTTP POST request with resilience policies and logging
    /// </summary>
    protected async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        Logger.LogInformation(
            "[{RequestId}] Starting {OperationName} POST request to {Url}",
            requestId, operationName, url);

        try
        {
            var requestContent = SerializeRequest(request);

            using var response = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                var httpResponse = await HttpClient.PostAsync(url, requestContent, cancellationToken);
                await EnsureSuccessStatusCodeAsync(httpResponse, url, requestId);
                return httpResponse;
            });

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = DeserializeResponse<TResponse>(content, url, requestId);

            stopwatch.Stop();

            Logger.LogInformation(
                "[{RequestId}] {OperationName} completed successfully in {ElapsedMs}ms",
                requestId, operationName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} HTTP request failed after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} timed out after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (ApiClientException)
        {
            stopwatch.Stop();
            throw;
        }
    }

    /// <summary>
    /// Makes an HTTP PUT request with resilience policies and logging
    /// </summary>
    protected async Task<TResponse> PutAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        Logger.LogInformation(
            "[{RequestId}] Starting {OperationName} PUT request to {Url}",
            requestId, operationName, url);

        try
        {
            var requestContent = SerializeRequest(request);

            using var response = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                var httpResponse = await HttpClient.PutAsync(url, requestContent, cancellationToken);
                await EnsureSuccessStatusCodeAsync(httpResponse, url, requestId);
                return httpResponse;
            });

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = DeserializeResponse<TResponse>(content, url, requestId);

            stopwatch.Stop();

            Logger.LogInformation(
                "[{RequestId}] {OperationName} completed successfully in {ElapsedMs}ms",
                requestId, operationName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} HTTP request failed after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} timed out after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (ApiClientException)
        {
            stopwatch.Stop();
            throw;
        }
    }

    /// <summary>
    /// Makes an HTTP DELETE request with resilience policies and logging
    /// </summary>
    protected async Task<TResponse> DeleteAsync<TResponse>(
        string url,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        Logger.LogInformation(
            "[{RequestId}] Starting {OperationName} DELETE request to {Url}",
            requestId, operationName, url);

        try
        {
            using var response = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                var httpResponse = await HttpClient.DeleteAsync(url, cancellationToken);
                await EnsureSuccessStatusCodeAsync(httpResponse, url, requestId);
                return httpResponse;
            });

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = DeserializeResponse<TResponse>(content, url, requestId);

            stopwatch.Stop();

            Logger.LogInformation(
                "[{RequestId}] {OperationName} completed successfully in {ElapsedMs}ms. Response size: {ResponseSize} bytes",
                requestId, operationName, stopwatch.ElapsedMilliseconds, content.Length);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} HTTP request failed after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{RequestId}] {OperationName} timed out after {ElapsedMs}ms. URL: {Url}",
                requestId, operationName, stopwatch.ElapsedMilliseconds, url);
            throw new ApiClientException(operationName, url, requestId, ex);
        }
        catch (ApiClientException)
        {
            stopwatch.Stop();
            throw;
        }
    }

    /// <summary>
    /// Builds the Polly resilience policy with retry, circuit breaker, and timeout
    /// </summary>
    private IAsyncPolicy<HttpResponseMessage> BuildResiliencePolicy()
    {
        // Retry policy with exponential backoff
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx and 408
            .Or<TimeoutException>()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests) // Handle 429
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    Logger.LogWarning(
                        "Retrying request (attempt {RetryAttempt}) after {DelaySeconds}s delay",
                        retryAttempt, delay.TotalSeconds);
                    return delay;
                },
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    if (outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = outcome.Result.Headers.RetryAfter?.Delta ?? timespan;
                        Logger.LogWarning(
                            "Rate limited (429). Retry attempt {RetryAttempt}. Waiting {RetryAfter}s",
                            retryAttempt, retryAfter.TotalSeconds);
                    }
                    else if (outcome.Exception != null)
                    {
                        Logger.LogWarning(outcome.Exception,
                            "Request failed with exception. Retry attempt {RetryAttempt}",
                            retryAttempt);
                    }
                });

        // Circuit breaker policy
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Logger.LogError(
                        "Circuit breaker opened for {DurationSeconds}s after {ConsecutiveFailures} consecutive failures",
                        duration.TotalSeconds, 5);
                },
                onReset: () =>
                {
                    Logger.LogInformation("Circuit breaker reset - service is healthy again");
                },
                onHalfOpen: () =>
                {
                    Logger.LogInformation("Circuit breaker half-open - testing service health");
                });

        // Timeout policy
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(30),
            onTimeoutAsync: (context, timespan, task) =>
            {
                Logger.LogWarning("Request timed out after {TimeoutSeconds}s", timespan.TotalSeconds);
                return Task.CompletedTask;
            });

        // Combine policies: Timeout -> Retry -> Circuit Breaker
        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    /// <summary>
    /// Ensures HTTP response is successful, otherwise throws detailed exception
    /// </summary>
    private async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        string url,
        Guid requestId)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();

        Logger.LogError(
            "[{RequestId}] HTTP request failed. Status: {StatusCode}, URL: {Url}, Response: {Response}",
            requestId, response.StatusCode, url, content);

        throw new HttpRequestException(
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. URL: {url}. Response: {content}",
            null,
            response.StatusCode);
    }

    /// <summary>
    /// Deserializes JSON response with error handling
    /// </summary>
    private TResponse DeserializeResponse<TResponse>(string content, string url, Guid requestId)
    {
        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);

            if (result == null)
            {
                throw new ApiClientException(
                    "Deserialization returned null",
                    url,
                    requestId,
                    new InvalidOperationException("Deserialized response is null"));
            }

            return result;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex,
                "[{RequestId}] Failed to deserialize response from {Url}. Content: {Content}",
                requestId, url, content);

            throw new ApiClientException(
                "JSON deserialization failed",
                url,
                requestId,
                ex);
        }
    }

    /// <summary>
    /// Serializes request object to JSON content
    /// </summary>
    private StringContent SerializeRequest<TRequest>(TRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to serialize request object of type {RequestType}", typeof(TRequest).Name);
            throw new ApiClientException("JSON serialization failed", string.Empty, Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Helper method to build query string from dictionary
    /// </summary>
    protected string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters == null || !parameters.Any())
        {
            return string.Empty;
        }

        var queryParams = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");

        return string.Join("&", queryParams);
    }
}

/// <summary>
/// Exception thrown by API clients
/// </summary>
public class ApiClientException : Exception
{
    public string OperationName { get; }
    public string Url { get; }
    public Guid RequestId { get; }

    public ApiClientException(
        string operationName,
        string url,
        Guid requestId,
        Exception innerException)
        : base($"API operation '{operationName}' failed. URL: {url}, RequestId: {requestId}", innerException)
    {
        OperationName = operationName;
        Url = url;
        RequestId = requestId;
    }
}
