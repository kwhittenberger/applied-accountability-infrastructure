# Applied Accountability Infrastructure - Integration Guide

This document provides comprehensive guidance for integrating the Applied Accountability Infrastructure library into your .NET applications.

## Table of Contents

- [What is Applied Accountability Infrastructure?](#what-is-applied-accountability-infrastructure)
- [Quick Start](#quick-start)
- [HTTP Client Integration](#http-client-integration)
- [Resilience Patterns](#resilience-patterns)
- [Logging and Monitoring](#logging-and-monitoring)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)

## What is Applied Accountability Infrastructure?

Applied Accountability Infrastructure is a **reusable library of enterprise-grade infrastructure components** for .NET applications. It provides battle-tested patterns and implementations for common infrastructure needs.

### Current Features

- **HTTP Client with Resilience**: Base client with automatic retry, circuit breaker, and timeout
- **Comprehensive Logging**: Structured logging with request IDs and metrics
- **Helper Utilities**: Query string building, safe data extraction, JSON serialization

### Planned Features

- **Distributed Caching**: Redis-backed cache helpers
- **Validation Framework**: FluentValidation extensions
- **Serialization Helpers**: Custom JSON converters
- **Mapping Utilities**: AutoMapper profiles

## Quick Start

### 1. Add Project Reference

In your application's `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\applied-accountability-infrastructure\src\AppliedAccountability.Infrastructure\AppliedAccountability.Infrastructure.csproj" />
</ItemGroup>
```

**Note**: Adjust the path based on your repository structure. The library is referenced as a project, not a NuGet package.

### 2. Create an API Client

Create a client class that inherits from `BaseApiClient`:

```csharp
using AppliedAccountability.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace YourApp.Infrastructure.Clients;

public class ExternalApiClient : BaseApiClient
{
    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<UserResponse> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.example.com/users/{userId}";
        return await GetAsync<UserResponse>(url, "GetUser", cancellationToken);
    }

    public async Task<CreateUserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var url = "https://api.example.com/users";
        return await PostAsync<CreateUserRequest, CreateUserResponse>(
            url,
            request,
            "CreateUser",
            cancellationToken);
    }
}
```

### 3. Register in Dependency Injection

In your `Program.cs`:

```csharp
// Configure named HttpClient
builder.Services.AddHttpClient<ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "YourApp/1.0");
});

// Client is now available via DI
```

### 4. Use in Your Services

```csharp
public class UserService
{
    private readonly ExternalApiClient _apiClient;
    private readonly ILogger<UserService> _logger;

    public UserService(ExternalApiClient apiClient, ILogger<UserService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<User> GetUserByIdAsync(int userId)
    {
        try
        {
            var response = await _apiClient.GetUserAsync(userId);
            return response.User;
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex,
                "Failed to fetch user {UserId} from external API. RequestId: {RequestId}",
                userId,
                ex.RequestId);
            throw;
        }
    }
}
```

## HTTP Client Integration

### BaseApiClient Overview

The `BaseApiClient` abstract class provides:

- **Resilience**: Automatic retry, circuit breaker, and timeout via Polly
- **Logging**: Structured logs with request IDs and timing
- **Error Handling**: Custom `ApiClientException` with context
- **JSON Support**: Preconfigured serialization settings

### Available HTTP Methods

#### GET Requests

```csharp
// Simple GET
public async Task<Product> GetProductAsync(string productId)
{
    var url = $"/api/products/{productId}";
    return await GetAsync<Product>(url, "GetProduct");
}

// GET with query parameters
public async Task<ProductList> SearchProductsAsync(string query, int page, int pageSize)
{
    var queryString = BuildQueryString(new Dictionary<string, string>
    {
        ["q"] = query,
        ["page"] = page.ToString(),
        ["pageSize"] = pageSize.ToString()
    });

    var url = $"/api/products/search?{queryString}";
    return await GetAsync<ProductList>(url, "SearchProducts");
}
```

#### POST Requests

```csharp
// POST with request/response bodies
public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
{
    var url = "/api/orders";
    return await PostAsync<CreateOrderRequest, OrderResponse>(
        url,
        request,
        "CreateOrder");
}

// POST with no response body (returns bool)
public async Task<bool> SendNotificationAsync(NotificationRequest request)
{
    var url = "/api/notifications";
    return await PostAsync<NotificationRequest>(
        url,
        request,
        "SendNotification");
}
```

### PUT, PATCH, DELETE (Planned)

**Status**: Not yet implemented - planned for future release.

**Current Workaround**: Use `HttpClient` directly in your derived class:

```csharp
public async Task<UpdateResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
{
    var url = $"/api/users/{userId}";
    var json = JsonSerializer.Serialize(request, JsonOptions);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await HttpClient.PutAsync(url, content);
    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<UpdateResponse>(responseJson, JsonOptions);
}
```

### Helper Methods

#### BuildQueryString

```csharp
var queryString = BuildQueryString(new Dictionary<string, string>
{
    ["search"] = "laptop",
    ["category"] = "electronics",
    ["minPrice"] = "100",
    ["maxPrice"] = "1000"
});
// Result: "search=laptop&category=electronics&minPrice=100&maxPrice=1000"
```

#### Safe Data Extraction

When working with dynamic JSON responses or dictionaries:

```csharp
var responseData = new Dictionary<string, object>
{
    ["userId"] = 12345,
    ["userName"] = "john.doe",
    ["createdAt"] = "2025-01-15T10:30:00Z",
    ["isActive"] = true,
    ["score"] = 95.5
};

// Safe extraction with type conversion
var userId = GetIntValue(responseData, "userId");           // 12345
var userName = GetStringValue(responseData, "userName");    // "john.doe"
var createdAt = GetDateTimeValue(responseData, "createdAt"); // DateTime (UTC)
var isActive = GetBoolValue(responseData, "isActive");      // true
var score = GetDoubleValue(responseData, "score");          // 95.5

// Returns null if key doesn't exist or conversion fails
var missing = GetIntValue(responseData, "nonExistent");     // null
```

## Resilience Patterns

BaseApiClient uses Polly to implement three resilience patterns:

### 1. Retry Policy

**Configuration:**
- **Attempts**: 3 retries (4 total attempts including initial)
- **Backoff**: Exponential (1s, 2s, 4s)
- **Triggers**:
  - HTTP 5xx (server errors)
  - HTTP 408 (request timeout)
  - HTTP 429 (too many requests)
  - `TaskCanceledException` / `TimeoutException`

**Behavior:**
```
Attempt 1: Request fails with 503 Service Unavailable
          → Wait 1 second
Attempt 2: Request fails with 503 Service Unavailable
          → Wait 2 seconds
Attempt 3: Request fails with 503 Service Unavailable
          → Wait 4 seconds
Attempt 4: Request fails with 503 Service Unavailable
          → Throw ApiClientException
```

**Logging**: Each retry attempt is logged with delay duration.

### 2. Circuit Breaker

**Configuration:**
- **Failure Threshold**: 5 consecutive failures
- **Break Duration**: 30 seconds
- **States**: Closed → Open → Half-Open → Closed

**Behavior:**
```
State: Closed (normal operation)
  ↓ 5 consecutive failures
State: Open (all requests fail immediately)
  ↓ After 30 seconds
State: Half-Open (test with 1 request)
  ↓ Success: → Closed
  ↓ Failure: → Open (reset timer)
```

**Benefits:**
- Prevents cascading failures
- Gives failing service time to recover
- Fails fast when service is known to be down

### 3. Timeout Policy

**Configuration:**
- **Duration**: 30 seconds per request
- **Applies To**: Individual HTTP requests, not including retries

**Example Timeline:**
```
0s   : Request starts
29s  : Response received → Success
30s  : Timeout → Triggers retry
```

### Combined Policy Behavior

All three policies work together:

```
Request 1: Timeout after 30s → Retry
Request 2: 503 error → Retry after 1s
Request 3: 503 error → Retry after 2s
Request 4: 503 error → Retry after 4s
Request 5: 503 error → Circuit breaks

Next 5 requests: Fail immediately (circuit open)
After 30s: Circuit half-open, allows 1 test request
```

## Logging and Monitoring

### Structured Logging

Every HTTP operation produces structured logs with correlation:

```csharp
// Request start
[dbf3e527-4a1c-4f2f-9a8b-1234567890ab] Starting GetUser GET request to https://api.example.com/users/123

// Successful response
[dbf3e527-4a1c-4f2f-9a8b-1234567890ab] GetUser completed successfully in 245ms. Response size: 1524 bytes

// Error
[dbf3e527-4a1c-4f2f-9a8b-1234567890ab] GetUser failed after 2 retry attempts. Status: 503. RequestId: dbf3e527-4a1c-4f2f-9a8b-1234567890ab
```

### Log Levels

- **Information**: Request start, successful completion
- **Warning**: Retry attempts, timeouts, rate limiting
- **Error**: Final failures, exceptions

### Metrics Available

From logs, you can extract:
- **Request duration** (milliseconds)
- **Response size** (bytes)
- **Success rate** (successful vs. failed)
- **Retry frequency** (how often retries occur)
- **Circuit breaker state changes**

### Request IDs

Every request gets a unique GUID for tracing:
- Logged at start and completion
- Included in `ApiClientException`
- Useful for correlating logs across systems

## Best Practices

### 1. Use Named HttpClients

**Good**:
```csharp
builder.Services.AddHttpClient<ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(100); // Overall timeout
});
```

**Avoid**:
```csharp
// Don't create HttpClient instances manually
var client = new HttpClient(); // ❌ Creates socket exhaustion
```

### 2. Handle ApiClientException

Always catch and handle the custom exception:

```csharp
try
{
    var result = await _apiClient.GetDataAsync();
    return result;
}
catch (ApiClientException ex)
{
    _logger.LogError(ex,
        "API call failed. Operation: {Operation}, RequestId: {RequestId}, Url: {Url}",
        ex.OperationName,
        ex.RequestId,
        ex.Url);

    // Decide: retry, fallback, or rethrow
    throw;
}
```

### 3. Use CancellationTokens

Pass cancellation tokens to support timeouts and cancellation:

```csharp
public async Task<Data> GetDataAsync(CancellationToken cancellationToken)
{
    return await _apiClient.FetchDataAsync(cancellationToken);
}
```

### 4. Configure Appropriate Timeouts

```csharp
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(100); // Overall timeout
    // BaseApiClient has 30s per-request timeout (via Polly)
    // Total: 100s overall, 30s per attempt
});
```

### 5. Set BaseAddress for Cleaner URLs

```csharp
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

// In your client, use relative URLs
public async Task<User> GetUserAsync(int id)
{
    // Combines with BaseAddress → https://api.example.com/users/123
    return await GetAsync<User>($"/users/{id}", "GetUser");
}
```

### 6. Add Authentication Headers

```csharp
builder.Services.AddHttpClient<SecureApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddHttpMessageHandler(sp =>
{
    var tokenProvider = sp.GetRequiredService<ITokenProvider>();
    return new AuthenticationHandler(tokenProvider);
});
```

### 7. Monitor Circuit Breaker State

The circuit breaker logs state changes. Monitor these in production:

```csharp
// Log analytics query
LogLevel == "Warning"
AND Message contains "Circuit breaker"
```

## Troubleshooting

### All Requests Failing Immediately

**Symptom**: Requests fail instantly without attempting to connect.

**Diagnosis**:
```csharp
// Check logs for circuit breaker messages
"Circuit breaker is open, failing fast"
```

**Cause**: Circuit breaker is in "Open" state after 5 consecutive failures.

**Solution**:
- Wait 30 seconds for circuit to half-open
- Fix underlying service issues
- Consider increasing failure threshold if transient issues are common

### Requests Timing Out

**Symptom**: Requests always timeout after 30 seconds.

**Diagnosis**:
```csharp
// Check logs
"Request timed out after 30000ms"
```

**Causes**:
1. External API is slow
2. Network latency
3. Large response payloads

**Solutions**:
- Increase timeout in derived class (override default)
- Optimize external API
- Use pagination for large datasets
- Consider async polling for long operations

### Too Many Retries

**Symptom**: Requests retry multiple times unnecessarily.

**Diagnosis**: Check retry attempt logs.

**Cause**: External API returns retriable status codes (5xx) frequently.

**Solution**:
- Investigate root cause at external API
- Consider custom retry policy for specific operations
- Add circuit breaker monitoring

### Deserialization Errors

**Symptom**: `JsonException` when parsing responses.

**Diagnosis**:
```csharp
catch (JsonException ex)
{
    _logger.LogError(ex, "Failed to deserialize response");
}
```

**Causes**:
1. API contract changed
2. Unexpected null values
3. Date format mismatch

**Solutions**:
- Use nullable reference types in DTOs
- Add custom JSON converters
- Validate API contract with integration tests

## Roadmap

### Upcoming Features (Priority Order)

#### 1. Additional HTTP Methods
**Status**: Planned
**ETA**: Next release

- `PutAsync<TRequest, TResponse>` - Update operations
- `PatchAsync<TRequest, TResponse>` - Partial updates
- `DeleteAsync` and `DeleteAsync<TResponse>` - Delete operations

#### 2. Unit Tests
**Status**: Planned
**ETA**: Next release

- Comprehensive test suite with xUnit
- Mock HTTP handlers for testing
- Polly policy verification
- Edge case coverage

#### 3. CI/CD Pipeline
**Status**: Planned
**ETA**: Next release

- GitHub Actions workflow
- Automated testing on commit
- Code coverage reporting
- NuGet package publication

#### 4. Distributed Caching
**Status**: Planned
**ETA**: Future release

- Redis cache wrapper
- Memory cache fallback
- Cache invalidation patterns
- Distributed lock support

#### 5. Validation Framework
**Status**: Planned
**ETA**: Future release

- FluentValidation extensions
- Common validation rules
- Integration with ASP.NET Core

#### 6. Serialization Helpers
**Status**: Planned
**ETA**: Future release

- Custom JSON converters
- XML serialization support
- Binary serialization utilities

### Migration Path

As features are added, integration will remain backwards compatible:

```csharp
// Current (v1.0)
var result = await GetAsync<User>(url, "GetUser");

// Future (v1.1) - new methods, existing still work
var result = await PutAsync<UpdateUser, User>(url, request, "UpdateUser");
```

## Integration Checklist

When integrating into a new project:

- [ ] Add project reference to `.csproj`
- [ ] Create client classes inheriting from `BaseApiClient`
- [ ] Register HttpClient in DI with `AddHttpClient<T>`
- [ ] Configure base address and default headers
- [ ] Implement API methods using GET/POST
- [ ] Add error handling with `ApiClientException`
- [ ] Use cancellation tokens
- [ ] Add structured logging
- [ ] Set up monitoring for circuit breaker events
- [ ] Test resilience patterns (retry, timeout, circuit breaker)
- [ ] Document API client usage for your team

## Examples

### Complete Integration Example

```csharp
// 1. Define DTOs
public record UserResponse(int Id, string Name, string Email);
public record CreateUserRequest(string Name, string Email);

// 2. Create client
public class UserApiClient : BaseApiClient
{
    public UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<UserResponse> GetUserAsync(int id, CancellationToken ct = default)
    {
        return await GetAsync<UserResponse>($"/users/{id}", "GetUser", ct);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        return await PostAsync<CreateUserRequest, UserResponse>("/users", request, "CreateUser", ct);
    }
}

// 3. Register in DI (Program.cs)
builder.Services.AddHttpClient<UserApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
});

// 4. Use in service
public class UserService
{
    private readonly UserApiClient _apiClient;

    public UserService(UserApiClient apiClient) => _apiClient = apiClient;

    public async Task<UserResponse> RegisterUserAsync(string name, string email)
    {
        try
        {
            var request = new CreateUserRequest(name, email);
            return await _apiClient.CreateUserAsync(request);
        }
        catch (ApiClientException ex)
        {
            // Handle error
            throw new ApplicationException($"User registration failed: {ex.Message}", ex);
        }
    }
}
```

## Support and Contribution

This library is maintained by **Applied Accountability Services LLC**.

For integration assistance:
- Review this guide and the README.md
- Check the troubleshooting section
- Examine the BaseApiClient source code
- Open an issue on GitHub for bugs or feature requests

## License

MIT License - Copyright © Applied Accountability Services LLC 2025
