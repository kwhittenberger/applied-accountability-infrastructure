using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppliedAccountability.Infrastructure.Serialization;

/// <summary>
/// Helper class for JSON serialization with custom converters and options
/// </summary>
public static class JsonSerializationHelper
{
    /// <summary>
    /// Default JSON options with common settings
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// Pretty-printed JSON options
    /// </summary>
    public static JsonSerializerOptions PrettyOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// Strict JSON options (fails on unknown properties)
    /// </summary>
    public static JsonSerializerOptions StrictOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>
    /// Serializes an object to JSON string
    /// </summary>
    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? DefaultOptions);
    }

    /// <summary>
    /// Serializes an object to JSON string asynchronously
    /// </summary>
    public static async Task<string> SerializeAsync<T>(T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, options ?? DefaultOptions, cancellationToken);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Deserializes JSON string to object
    /// </summary>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes JSON string to object asynchronously
    /// </summary>
    public static async Task<T?> DeserializeAsync<T>(string json, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return await JsonSerializer.DeserializeAsync<T>(stream, options ?? DefaultOptions, cancellationToken);
    }

    /// <summary>
    /// Safely deserializes JSON, returning default value on failure
    /// </summary>
    public static T? TryDeserialize<T>(string json, T? defaultValue = default, JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
        catch (JsonException)
        {
            return defaultValue;
        }
        catch (NotSupportedException)
        {
            return defaultValue;
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Deep clones an object via JSON serialization
    /// </summary>
    public static T? Clone<T>(T value, JsonSerializerOptions? options = null)
    {
        var json = Serialize(value, options);
        return Deserialize<T>(json, options);
    }

    /// <summary>
    /// Converts an object to a dictionary
    /// </summary>
    public static Dictionary<string, object?>? ToDictionary<T>(T value, JsonSerializerOptions? options = null)
    {
        var json = Serialize(value, options);
        return Deserialize<Dictionary<string, object?>>(json, options);
    }

    /// <summary>
    /// Converts a dictionary to an object
    /// </summary>
    public static T? FromDictionary<T>(Dictionary<string, object?> dictionary, JsonSerializerOptions? options = null)
    {
        var json = Serialize(dictionary, options);
        return Deserialize<T>(json, options);
    }
}

/// <summary>
/// Custom JSON converter for UTC DateTime serialization
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTime = reader.GetDateTime();
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcDateTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utcDateTime);
    }
}

/// <summary>
/// Custom JSON converter for nullable UTC DateTime serialization
/// </summary>
public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var dateTime = reader.GetDateTime();
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var utcDateTime = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
        writer.WriteStringValue(utcDateTime);
    }
}

/// <summary>
/// Custom JSON converter for trimming string values
/// </summary>
public class TrimmingStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.Trim();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.Trim());
    }
}
