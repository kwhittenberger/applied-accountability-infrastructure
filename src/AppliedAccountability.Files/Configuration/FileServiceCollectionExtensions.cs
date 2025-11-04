using AppliedAccountability.Files.Abstractions;
using AppliedAccountability.Files.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppliedAccountability.Files.Configuration;

/// <summary>
/// Extension methods for registering file storage services.
/// </summary>
public static class FileServiceCollectionExtensions
{
    /// <summary>
    /// Adds file storage services to the service collection.
    /// </summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    /// <summary>
    /// Adds local file storage.
    /// </summary>
    public static IServiceCollection AddLocalFileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "LocalFileStorage")
    {
        services.Configure<LocalFileStorageSettings>(configuration.GetSection(configSectionName));

        services.AddSingleton<IFileStorage>(provider =>
        {
            var settings = new LocalFileStorageSettings
            {
                RootPath = configuration[$"{configSectionName}:RootPath"]
                    ?? throw new InvalidOperationException($"{configSectionName}:RootPath is required")
            };

            var defaultContainer = configuration[$"{configSectionName}:DefaultContainer"];
            if (!string.IsNullOrWhiteSpace(defaultContainer))
            {
                settings.DefaultContainer = defaultContainer;
            }

            return new LocalFileStorage(settings);
        });

        return services;
    }

    /// <summary>
    /// Adds local file storage with explicit settings.
    /// </summary>
    public static IServiceCollection AddLocalFileStorage(
        this IServiceCollection services,
        Action<LocalFileStorageSettings> configureSettings)
    {
        var settings = new LocalFileStorageSettings { RootPath = string.Empty };
        configureSettings(settings);

        services.AddSingleton<IFileStorage>(new LocalFileStorage(settings));

        return services;
    }

    /// <summary>
    /// Adds MinIO/S3-compatible file storage.
    /// </summary>
    public static IServiceCollection AddMinioFileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "MinIO")
    {
        services.Configure<MinioFileStorageSettings>(configuration.GetSection(configSectionName));

        services.AddSingleton<IFileStorage>(provider =>
        {
            var settings = new MinioFileStorageSettings
            {
                Endpoint = configuration[$"{configSectionName}:Endpoint"]
                    ?? throw new InvalidOperationException($"{configSectionName}:Endpoint is required")
            };

            var accessKey = configuration[$"{configSectionName}:AccessKey"];
            if (!string.IsNullOrWhiteSpace(accessKey))
            {
                settings.AccessKey = accessKey;
            }

            var secretKey = configuration[$"{configSectionName}:SecretKey"];
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                settings.SecretKey = secretKey;
            }

            var useSSL = configuration[$"{configSectionName}:UseSSL"];
            if (bool.TryParse(useSSL, out var useSslValue))
            {
                settings.UseSSL = useSslValue;
            }

            var region = configuration[$"{configSectionName}:Region"];
            if (!string.IsNullOrWhiteSpace(region))
            {
                settings.Region = region;
            }

            var defaultBucket = configuration[$"{configSectionName}:DefaultBucket"];
            if (!string.IsNullOrWhiteSpace(defaultBucket))
            {
                settings.DefaultBucket = defaultBucket;
            }

            return new MinioFileStorage(settings);
        });

        return services;
    }

    /// <summary>
    /// Adds MinIO/S3-compatible file storage with explicit settings.
    /// </summary>
    public static IServiceCollection AddMinioFileStorage(
        this IServiceCollection services,
        Action<MinioFileStorageSettings> configureSettings)
    {
        var settings = new MinioFileStorageSettings { Endpoint = string.Empty };
        configureSettings(settings);

        services.AddSingleton<IFileStorage>(new MinioFileStorage(settings));

        return services;
    }
}
