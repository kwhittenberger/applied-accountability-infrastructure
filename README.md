# AppliedAccountability.Infrastructure

Enterprise-grade infrastructure components for .NET applications.

## Features

### HTTP Client with Resilience (`AppliedAccountability.Infrastructure.Http`)

Robust HTTP client base class with built-in Polly resilience patterns:

- **Automatic Retry**: Exponential backoff for transient failures (5xx, 408, 429)
- **Circuit Breaker**: Prevents cascading failures (5 failures → 30s break)
- **Timeout Protection**: 30-second timeout for all requests
- **Comprehensive Logging**: Request IDs, timing metrics, detailed error context
- **Rate Limit Handling**: Automatic retry-after header support
- **JSON Serialization**: Preconfigured with sensible defaults

## Installation

```bash
dotnet add package AppliedAccountability.Infrastructure
```

## Usage

### Creating an HTTP Client

```csharp
using AppliedAccountability.Infrastructure.Http;
using Microsoft.Extensions.Logging;

public class MyApiClient : BaseApiClient
{
    public MyApiClient(HttpClient httpClient, ILogger<MyApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<UserResponse> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.example.com/users/{userId}";
        return await GetAsync<UserResponse>(url, "GetUser", cancellationToken);
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var url = "https://api.example.com/users";
        return await PostAsync<CreateUserRequest, CreateUserResponse>(url, request, "CreateUser", cancellationToken);
    }
}
```

### Helper Methods

```csharp
// Query string building
var queryString = BuildQueryString(new Dictionary<string, string>
{
    ["search"] = "test",
    ["limit"] = "10"
});
// Result: "search=test&limit=10"

// Safe data extraction
var data = new Dictionary<string, object>
{
    ["name"] = "John Doe",
    ["age"] = 30,
    ["created"] = "2025-01-15T10:30:00Z"
};

var name = GetStringValue(data, "name");         // "John Doe"
var age = GetIntValue(data, "age");              // 30
var created = GetDateTimeValue(data, "created"); // DateTime (UTC)
```

## Resilience Policies

### Retry Policy
- **Attempts**: 3 retries
- **Backoff**: Exponential (1s, 2s, 4s)
- **Triggers**: 5xx, 408, 429, TimeoutException

### Circuit Breaker
- **Threshold**: 5 consecutive failures
- **Break Duration**: 30 seconds
- **Half-Open**: Automatic health testing

### Timeout
- **Duration**: 30 seconds per request
- **Logging**: Timeout events logged as warnings

## Logging

All HTTP operations produce structured logs with:
- Request ID (GUID) for tracing
- Operation name
- Timing metrics (elapsed milliseconds)
- Response size
- Error details with full context

Example log output:
```
[a1b2c3d4-e5f6-7890] Starting GetUser GET request to https://api.example.com/users/123
[a1b2c3d4-e5f6-7890] GetUser completed successfully in 245ms. Response size: 1524 bytes
```

## Future Components

Planned additions to this package:
- **Caching**: Distributed cache helpers with Redis support
- **Serialization**: Custom JSON converters and helpers
- **Validation**: FluentValidation extensions
- **Mapping**: AutoMapper profiles and extensions

## Requirements

- .NET 9.0 or later
- Microsoft.Extensions.Http.Polly 9.0.10+
- Microsoft.Extensions.Logging 9.0.10+

## License

MIT License - Copyright © Applied Accountability Services LLC 2025

## Contributing

This package is maintained by Applied Accountability Services LLC. For bug reports or feature requests, please open an issue on GitHub.
