using AppliedAccountability.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppliedAccountability.Infrastructure.Tests.Caching;

public class DistributedCacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<DistributedCacheService>> _mockLogger;
    private readonly DistributedCacheService _service;

    public DistributedCacheServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<DistributedCacheService>>();
        _service = new DistributedCacheService(_mockCache.Object, _mockLogger.Object, TimeSpan.FromMinutes(10));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DistributedCacheService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DistributedCacheService(_mockCache.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new DistributedCacheService(_mockCache.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetAsync<TestCacheObject>(null!));
    }

    [Fact]
    public async Task GetAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetAsync<TestCacheObject>(string.Empty));
    }

    [Fact]
    public async Task GetAsync_WithValidKey_ReturnsDeserializedObject()
    {
        // Arrange
        var key = "test-key";
        var expectedObject = new TestCacheObject { Id = 1, Name = "Test" };
        var serializedData = System.Text.Json.JsonSerializer.Serialize(expectedObject);

        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedData);

        // Act
        var result = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedObject.Id, result.Id);
        Assert.Equal(expectedObject.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent-key";
        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WithInvalidJson_ReturnsNull()
    {
        // Arrange
        var key = "invalid-json-key";
        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("invalid json data");

        // Act
        var result = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_ReturnsNull()
    {
        // Arrange
        var key = "test-key";
        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.SetAsync(null!, value));
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.SetAsync<TestCacheObject>("test-key", null!));
    }

    [Fact]
    public async Task SetAsync_WithValidData_CallsCacheSet()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        _mockCache
            .Setup(c => c.SetStringAsync(
                key,
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SetAsync(key, value);

        // Assert
        _mockCache.Verify(c => c.SetStringAsync(
            key,
            It.IsAny<string>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_UsesProvidedExpiration()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        DistributedCacheEntryOptions? capturedOptions = null;
        _mockCache
            .Setup(c => c.SetStringAsync(
                key,
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, DistributedCacheEntryOptions, CancellationToken>(
                (_, _, options, _) => capturedOptions = options)
            .Returns(Task.CompletedTask);

        // Act
        await _service.SetAsync(key, value, expiration);

        // Assert
        Assert.NotNull(capturedOptions);
        Assert.Equal(expiration, capturedOptions.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task SetAsync_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        _mockCache
            .Setup(c => c.SetStringAsync(
                key,
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.SetAsync(key, value));
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.RemoveAsync(null!));
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_CallsCacheRemove()
    {
        // Arrange
        var key = "test-key";
        _mockCache
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RemoveAsync(key);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = "test-key";
        _mockCache
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.RemoveAsync(key));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.ExistsAsync(null!));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "existing-key";
        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("some data");

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existent-key";
        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithCancellationToken_ReturnsFalse()
    {
        // Arrange
        var key = "test-key";
        _mockCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetManyAsync Tests

    [Fact]
    public async Task GetManyAsync_WithNullKeys_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.GetManyAsync<TestCacheObject>(null!));
    }

    [Fact]
    public async Task GetManyAsync_WithValidKeys_ReturnsMultipleValues()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        var obj1 = new TestCacheObject { Id = 1, Name = "Test1" };
        var obj2 = new TestCacheObject { Id = 2, Name = "Test2" };

        _mockCache
            .Setup(c => c.GetStringAsync("key1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(obj1));
        _mockCache
            .Setup(c => c.GetStringAsync("key2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(obj2));
        _mockCache
            .Setup(c => c.GetStringAsync("key3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetManyAsync<TestCacheObject>(keys);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.NotNull(result["key1"]);
        Assert.NotNull(result["key2"]);
        Assert.Null(result["key3"]);
    }

    #endregion

    #region SetManyAsync Tests

    [Fact]
    public async Task SetManyAsync_WithNullItems_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.SetManyAsync<TestCacheObject>(null!));
    }

    [Fact]
    public async Task SetManyAsync_WithValidItems_SetsMultipleValues()
    {
        // Arrange
        var items = new Dictionary<string, TestCacheObject>
        {
            ["key1"] = new TestCacheObject { Id = 1, Name = "Test1" },
            ["key2"] = new TestCacheObject { Id = 2, Name = "Test2" }
        };

        _mockCache
            .Setup(c => c.SetStringAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SetManyAsync(items);

        // Assert
        _mockCache.Verify(c => c.SetStringAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region RemoveManyAsync Tests

    [Fact]
    public async Task RemoveManyAsync_WithNullKeys_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.RemoveManyAsync(null!));
    }

    [Fact]
    public async Task RemoveManyAsync_WithValidKeys_RemovesMultipleValues()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        _mockCache
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RemoveManyAsync(keys);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.RefreshAsync(null!));
    }

    [Fact]
    public async Task RefreshAsync_WithValidKey_CallsCacheRefresh()
    {
        // Arrange
        var key = "test-key";
        _mockCache
            .Setup(c => c.RefreshAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RefreshAsync(key);

        // Assert
        _mockCache.Verify(c => c.RefreshAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = "test-key";
        _mockCache
            .Setup(c => c.RefreshAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.RefreshAsync(key));
    }

    #endregion

    #region CacheException Tests

    [Fact]
    public void CacheException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Cache error";
        var innerException = new Exception("Inner error");

        // Act
        var exception = new CacheException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    #endregion
}

// Test helper class
public class TestCacheObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
