using System.Text.Json;
using AppliedAccountability.Infrastructure.Serialization;

namespace AppliedAccountability.Infrastructure.Tests.Serialization;

public class JsonSerializationHelperTests
{
    #region DefaultOptions Tests

    [Fact]
    public void DefaultOptions_HasCorrectSettings()
    {
        // Arrange & Act
        var options = JsonSerializationHelper.DefaultOptions;

        // Assert
        Assert.True(options.PropertyNameCaseInsensitive);
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.False(options.WriteIndented);
    }

    #endregion

    #region PrettyOptions Tests

    [Fact]
    public void PrettyOptions_HasCorrectSettings()
    {
        // Arrange & Act
        var options = JsonSerializationHelper.PrettyOptions;

        // Assert
        Assert.True(options.PropertyNameCaseInsensitive);
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.True(options.WriteIndented);
    }

    #endregion

    #region StrictOptions Tests

    [Fact]
    public void StrictOptions_HasCorrectSettings()
    {
        // Arrange & Act
        var options = JsonSerializationHelper.StrictOptions;

        // Assert
        Assert.False(options.PropertyNameCaseInsensitive);
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.False(options.WriteIndented);
    }

    #endregion

    #region Serialize Tests

    [Fact]
    public void Serialize_WithValidObject_ReturnsJsonString()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test", IsActive = true };

        // Act
        var json = JsonSerializationHelper.Serialize(obj);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"id\":1", json);
        Assert.Contains("\"name\":\"Test\"", json);
        Assert.Contains("\"isActive\":true", json);
    }

    [Fact]
    public void Serialize_WithNullProperties_OmitsNullValues()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = null, IsActive = true };

        // Act
        var json = JsonSerializationHelper.Serialize(obj);

        // Assert
        Assert.NotNull(json);
        Assert.DoesNotContain("\"name\"", json);
    }

    [Fact]
    public void Serialize_WithPrettyOptions_ReturnsFormattedJson()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test", IsActive = true };

        // Act
        var json = JsonSerializationHelper.Serialize(obj, JsonSerializationHelper.PrettyOptions);

        // Assert
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    #endregion

    #region SerializeAsync Tests

    [Fact]
    public async Task SerializeAsync_WithValidObject_ReturnsJsonString()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test", IsActive = true };

        // Act
        var json = await JsonSerializationHelper.SerializeAsync(obj);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"id\":1", json);
    }

    [Fact]
    public async Task SerializeAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test", IsActive = true };
        var cts = new CancellationTokenSource();

        // Act
        var json = await JsonSerializationHelper.SerializeAsync(obj, cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(json);
    }

    #endregion

    #region Deserialize Tests

    [Fact]
    public void Deserialize_WithValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";

        // Act
        var obj = JsonSerializationHelper.Deserialize<TestObject>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
        Assert.Equal("Test", obj.Name);
        Assert.True(obj.IsActive);
    }

    [Fact]
    public void Deserialize_WithCaseInsensitiveJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"ID\":1,\"NAME\":\"Test\",\"ISACTIVE\":true}";

        // Act
        var obj = JsonSerializationHelper.Deserialize<TestObject>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
        Assert.Equal("Test", obj.Name);
        Assert.True(obj.IsActive);
    }

    [Fact]
    public void Deserialize_WithMissingProperties_ReturnsObjectWithDefaults()
    {
        // Arrange
        var json = "{\"id\":1}";

        // Act
        var obj = JsonSerializationHelper.Deserialize<TestObject>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
        Assert.Null(obj.Name);
        Assert.False(obj.IsActive);
    }

    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "invalid json";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializationHelper.Deserialize<TestObject>(json));
    }

    #endregion

    #region DeserializeAsync Tests

    [Fact]
    public async Task DeserializeAsync_WithValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";

        // Act
        var obj = await JsonSerializationHelper.DeserializeAsync<TestObject>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
        Assert.Equal("Test", obj.Name);
    }

    [Fact]
    public async Task DeserializeAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"Test\"}";
        var cts = new CancellationTokenSource();

        // Act
        var obj = await JsonSerializationHelper.DeserializeAsync<TestObject>(json, cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(obj);
    }

    #endregion

    #region TryDeserialize Tests

    [Fact]
    public void TryDeserialize_WithValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"Test\"}";

        // Act
        var obj = JsonSerializationHelper.TryDeserialize<TestObject>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
    }

    [Fact]
    public void TryDeserialize_WithInvalidJson_ReturnsDefault()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var obj = JsonSerializationHelper.TryDeserialize<TestObject>(json);

        // Assert
        Assert.Null(obj);
    }

    [Fact]
    public void TryDeserialize_WithInvalidJson_ReturnsProvidedDefault()
    {
        // Arrange
        var json = "invalid json";
        var defaultValue = new TestObject { Id = 999, Name = "Default" };

        // Act
        var obj = JsonSerializationHelper.TryDeserialize(json, defaultValue);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(999, obj.Id);
        Assert.Equal("Default", obj.Name);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_WithValidObject_ReturnsDeepCopy()
    {
        // Arrange
        var original = new TestObject { Id = 1, Name = "Original", IsActive = true };

        // Act
        var clone = JsonSerializationHelper.Clone(original);

        // Assert
        Assert.NotNull(clone);
        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.IsActive, clone.IsActive);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new TestObject { Id = 1, Name = "Original", IsActive = true };
        var clone = JsonSerializationHelper.Clone(original);

        // Act
        clone!.Name = "Modified";
        clone.IsActive = false;

        // Assert
        Assert.Equal("Original", original.Name);
        Assert.True(original.IsActive);
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_WithValidObject_ReturnsDictionary()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test", IsActive = true };

        // Act
        var dict = JsonSerializationHelper.ToDictionary(obj);

        // Assert
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey("id"));
        Assert.True(dict.ContainsKey("name"));
        Assert.True(dict.ContainsKey("isActive"));
    }

    [Fact]
    public void ToDictionary_WithCamelCaseProperties_UsesCamelCase()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test" };

        // Act
        var dict = JsonSerializationHelper.ToDictionary(obj);

        // Assert
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey("id"));
        Assert.False(dict.ContainsKey("Id"));
    }

    #endregion

    #region FromDictionary Tests

    [Fact]
    public void FromDictionary_WithValidDictionary_ReturnsObject()
    {
        // Arrange
        var dict = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Test",
            ["isActive"] = true
        };

        // Act
        var obj = JsonSerializationHelper.FromDictionary<TestObject>(dict);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
        Assert.Equal("Test", obj.Name);
        Assert.True(obj.IsActive);
    }

    [Fact]
    public void FromDictionary_WithMissingProperties_ReturnsObjectWithDefaults()
    {
        // Arrange
        var dict = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var obj = JsonSerializationHelper.FromDictionary<TestObject>(dict);

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, obj.Id);
        Assert.Null(obj.Name);
    }

    #endregion
}

public class UtcDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public UtcDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new UtcDateTimeConverter());
    }

    #region Read Tests

    [Fact]
    public void Read_WithUtcDateTime_ReturnsUtcDateTime()
    {
        // Arrange
        var json = "{\"date\":\"2025-01-15T10:30:00Z\"}";

        // Act
        var result = JsonSerializer.Deserialize<DateTimeWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public void Read_WithLocalDateTime_ConvertsToUtc()
    {
        // Arrange
        var localDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Local);
        var json = JsonSerializer.Serialize(new DateTimeWrapper { Date = localDate });

        // Act
        var result = JsonSerializer.Deserialize<DateTimeWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_WithUtcDateTime_WritesUtcDateTime()
    {
        // Arrange
        var wrapper = new DateTimeWrapper { Date = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc) };

        // Act
        var json = JsonSerializer.Serialize(wrapper, _options);

        // Assert
        Assert.Contains("2025-01-15", json);
        Assert.Contains("Z", json);
    }

    [Fact]
    public void Write_WithLocalDateTime_ConvertsAndWritesUtc()
    {
        // Arrange
        var wrapper = new DateTimeWrapper { Date = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Local) };

        // Act
        var json = JsonSerializer.Serialize(wrapper, _options);

        // Assert
        Assert.Contains("Z", json);
    }

    #endregion
}

public class NullableUtcDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public NullableUtcDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new NullableUtcDateTimeConverter());
    }

    #region Read Tests

    [Fact]
    public void Read_WithNull_ReturnsNull()
    {
        // Arrange
        var json = "{\"date\":null}";

        // Act
        var result = JsonSerializer.Deserialize<NullableDateTimeWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Date);
    }

    [Fact]
    public void Read_WithUtcDateTime_ReturnsUtcDateTime()
    {
        // Arrange
        var json = "{\"date\":\"2025-01-15T10:30:00Z\"}";

        // Act
        var result = JsonSerializer.Deserialize<NullableDateTimeWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Date);
        Assert.Equal(DateTimeKind.Utc, result.Date.Value.Kind);
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_WithNull_WritesNull()
    {
        // Arrange
        var wrapper = new NullableDateTimeWrapper { Date = null };

        // Act
        var json = JsonSerializer.Serialize(wrapper, _options);

        // Assert
        Assert.Contains("null", json);
    }

    [Fact]
    public void Write_WithUtcDateTime_WritesUtcDateTime()
    {
        // Arrange
        var wrapper = new NullableDateTimeWrapper
        {
            Date = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(wrapper, _options);

        // Assert
        Assert.Contains("2025-01-15", json);
        Assert.Contains("Z", json);
    }

    #endregion
}

public class TrimmingStringConverterTests
{
    private readonly JsonSerializerOptions _options;

    public TrimmingStringConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new TrimmingStringConverter());
    }

    #region Read Tests

    [Fact]
    public void Read_WithWhitespace_TrimsString()
    {
        // Arrange
        var json = "{\"value\":\"  test  \"}";

        // Act
        var result = JsonSerializer.Deserialize<StringWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Read_WithLeadingWhitespace_TrimsString()
    {
        // Arrange
        var json = "{\"value\":\"  test\"}";

        // Act
        var result = JsonSerializer.Deserialize<StringWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Read_WithTrailingWhitespace_TrimsString()
    {
        // Arrange
        var json = "{\"value\":\"test  \"}";

        // Act
        var result = JsonSerializer.Deserialize<StringWrapper>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Value);
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_WithWhitespace_TrimsString()
    {
        // Arrange
        var wrapper = new StringWrapper { Value = "  test  " };

        // Act
        var json = JsonSerializer.Serialize(wrapper, _options);

        // Assert
        Assert.Contains("\"test\"", json);
        Assert.DoesNotContain("  ", json);
    }

    [Fact]
    public void Write_WithoutWhitespace_WritesUnchanged()
    {
        // Arrange
        var wrapper = new StringWrapper { Value = "test" };

        // Act
        var json = JsonSerializer.Serialize(wrapper, _options);

        // Assert
        Assert.Contains("\"test\"", json);
    }

    #endregion
}

// Test helper classes
public class TestObject
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}

public class DateTimeWrapper
{
    public DateTime Date { get; set; }
}

public class NullableDateTimeWrapper
{
    public DateTime? Date { get; set; }
}

public class StringWrapper
{
    public string? Value { get; set; }
}
