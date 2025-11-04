using System.Net;
using System.Text;
using System.Text.Json;
using AppliedAccountability.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace AppliedAccountability.Infrastructure.Tests.Http;

public class BaseApiClientTests
{
    private readonly Mock<ILogger<TestApiClient>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly TestApiClient _client;

    public BaseApiClientTests()
    {
        _mockLogger = new Mock<ILogger<TestApiClient>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        _client = new TestApiClient(_httpClient, _mockLogger.Object);
    }

    #region GET Tests

    [Fact]
    public async Task GetAsync_SuccessfulRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var expectedResponse = new TestResponse { Id = 1, Name = "Test" };
        var responseContent = JsonSerializer.Serialize(expectedResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.TestGetAsync<TestResponse>("https://api.example.com/test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_NotFoundResponse_ThrowsApiClientException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Resource not found")
            });

        // Act & Assert
        await Assert.ThrowsAsync<ApiClientException>(
            async () => await _client.TestGetAsync<TestResponse>("https://api.example.com/test"));
    }

    [Fact]
    public async Task GetAsync_InvalidJson_ThrowsApiClientException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid JSON", Encoding.UTF8, "application/json")
            });

        // Act & Assert
        await Assert.ThrowsAsync<ApiClientException>(
            async () => await _client.TestGetAsync<TestResponse>("https://api.example.com/test"));
    }

    #endregion

    #region POST Tests

    [Fact]
    public async Task PostAsync_SuccessfulRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Id = 1, Name = "Test" };
        var responseContent = JsonSerializer.Serialize(expectedResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://api.example.com/test"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.TestPostAsync<TestRequest, TestResponse>(
            "https://api.example.com/test", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Name, result.Name);
    }

    [Fact]
    public async Task PostAsync_BadRequest_ThrowsApiClientException()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid request")
            });

        // Act & Assert
        await Assert.ThrowsAsync<ApiClientException>(
            async () => await _client.TestPostAsync<TestRequest, TestResponse>(
                "https://api.example.com/test", request));
    }

    #endregion

    #region PUT Tests

    [Fact]
    public async Task PutAsync_SuccessfulRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new TestRequest { Name = "Updated" };
        var expectedResponse = new TestResponse { Id = 1, Name = "Updated" };
        var responseContent = JsonSerializer.Serialize(expectedResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.TestPutAsync<TestRequest, TestResponse>(
            "https://api.example.com/test/1", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Name, result.Name);
    }

    [Fact]
    public async Task PutAsync_WithoutResponse_ReturnsTrue()
    {
        // Arrange
        var request = new TestRequest { Name = "Updated" };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _client.TestPutAsync<TestRequest>(
            "https://api.example.com/test/1", request);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region PATCH Tests

    [Fact]
    public async Task PatchAsync_SuccessfulRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new TestRequest { Name = "Patched" };
        var expectedResponse = new TestResponse { Id = 1, Name = "Patched" };
        var responseContent = JsonSerializer.Serialize(expectedResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.TestPatchAsync<TestRequest, TestResponse>(
            "https://api.example.com/test/1", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Name, result.Name);
    }

    [Fact]
    public async Task PatchAsync_WithoutResponse_ReturnsTrue()
    {
        // Arrange
        var request = new TestRequest { Name = "Patched" };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _client.TestPatchAsync<TestRequest>(
            "https://api.example.com/test/1", request);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeleteAsync_SuccessfulRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var expectedResponse = new TestResponse { Id = 1, Name = "Deleted" };
        var responseContent = JsonSerializer.Serialize(expectedResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.TestDeleteAsync<TestResponse>(
            "https://api.example.com/test/1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Name, result.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithoutResponse_ReturnsTrue()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _client.TestDeleteAsync("https://api.example.com/test/1");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void BuildQueryString_WithValidParameters_ReturnsFormattedString()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["name"] = "test",
            ["age"] = "25",
            ["city"] = "New York"
        };

        // Act
        var result = _client.TestBuildQueryString(parameters);

        // Assert
        Assert.Contains("name=test", result);
        Assert.Contains("age=25", result);
        Assert.Contains("city=New", result);
    }

    [Fact]
    public void BuildQueryString_WithEmptyDictionary_ReturnsEmptyString()
    {
        // Arrange
        var parameters = new Dictionary<string, string>();

        // Act
        var result = _client.TestBuildQueryString(parameters);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetStringValue_WithValidKey_ReturnsValue()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["name"] = "Test"
        };

        // Act
        var result = _client.TestGetStringValue(data, "name");

        // Assert
        Assert.Equal("Test", result);
    }

    [Fact]
    public void GetIntValue_WithValidKey_ReturnsValue()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["age"] = 25
        };

        // Act
        var result = _client.TestGetIntValue(data, "age");

        // Assert
        Assert.Equal(25, result);
    }

    [Fact]
    public void GetBoolValue_WithValidKey_ReturnsValue()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["isActive"] = true
        };

        // Act
        var result = _client.TestGetBoolValue(data, "isActive");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetDateTimeValue_WithValidKey_ReturnsUtcDateTime()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["created"] = "2025-01-15T10:30:00Z"
        };

        // Act
        var result = _client.TestGetDateTimeValue(data, "created");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
    }

    #endregion
}

// Test helper classes
public class TestApiClient : BaseApiClient
{
    public TestApiClient(HttpClient httpClient, ILogger<TestApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<TResponse> TestGetAsync<TResponse>(string url, CancellationToken cancellationToken = default)
        => GetAsync<TResponse>(url, "TestGet", cancellationToken);

    public Task<TResponse> TestPostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken = default)
        => PostAsync<TRequest, TResponse>(url, request, "TestPost", cancellationToken);

    public Task<TResponse> TestPutAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken = default)
        => PutAsync<TRequest, TResponse>(url, request, "TestPut", cancellationToken);

    public Task<bool> TestPutAsync<TRequest>(string url, TRequest request, CancellationToken cancellationToken = default)
        => PutAsync<TRequest>(url, request, "TestPut", cancellationToken);

    public Task<TResponse> TestPatchAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken = default)
        => PatchAsync<TRequest, TResponse>(url, request, "TestPatch", cancellationToken);

    public Task<bool> TestPatchAsync<TRequest>(string url, TRequest request, CancellationToken cancellationToken = default)
        => PatchAsync<TRequest>(url, request, "TestPatch", cancellationToken);

    public Task<TResponse> TestDeleteAsync<TResponse>(string url, CancellationToken cancellationToken = default)
        => DeleteAsync<TResponse>(url, "TestDelete", cancellationToken);

    public Task<bool> TestDeleteAsync(string url, CancellationToken cancellationToken = default)
        => DeleteAsync(url, "TestDelete", cancellationToken);

    public string TestBuildQueryString(Dictionary<string, string> parameters)
        => BuildQueryString(parameters);

    public string? TestGetStringValue(Dictionary<string, object> data, string key)
        => GetStringValue(data, key);

    public int? TestGetIntValue(Dictionary<string, object> data, string key)
        => GetIntValue(data, key);

    public bool? TestGetBoolValue(Dictionary<string, object> data, string key)
        => GetBoolValue(data, key);

    public DateTime? TestGetDateTimeValue(Dictionary<string, object> data, string key)
        => GetDateTimeValue(data, key);
}

public record TestRequest
{
    public string Name { get; init; } = string.Empty;
}

public record TestResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
