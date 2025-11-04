using AppliedAccountability.Files.Abstractions;
using AppliedAccountability.Files.Models;
using Minio;
using Minio.DataModel.Args;

namespace AppliedAccountability.Files.Providers;

/// <summary>
/// MinIO/S3-compatible storage implementation.
/// </summary>
public class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly MinioFileStorageSettings _settings;

    public MinioFileStorage(MinioFileStorageSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrWhiteSpace(_settings.Endpoint))
        {
            throw new ArgumentException("Endpoint is required", nameof(settings));
        }

        var builder = new MinioClient()
            .WithEndpoint(_settings.Endpoint);

        if (!string.IsNullOrWhiteSpace(_settings.AccessKey) && !string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            builder.WithCredentials(_settings.AccessKey, _settings.SecretKey);
        }

        if (_settings.UseSSL)
        {
            builder.WithSSL();
        }

        if (!string.IsNullOrWhiteSpace(_settings.Region))
        {
            builder.WithRegion(_settings.Region);
        }

        _client = builder.Build();
    }

    public async Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        var bucket = request.Container ?? _settings.DefaultBucket;
        var objectName = request.StoragePath ?? GenerateStoragePath(request.FileName);

        await EnsureBucketExistsAsync(bucket, cancellationToken);

        if (!request.Overwrite)
        {
            var exists = await ExistsAsync(objectName, bucket, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"File already exists: {objectName}");
            }
        }

        var putArgs = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(request.Stream)
            .WithObjectSize(request.Stream.Length)
            .WithContentType(request.ContentType);

        if (request.Metadata != null && request.Metadata.Count > 0)
        {
            putArgs.WithHeaders(request.Metadata);
        }

        var result = await _client.PutObjectAsync(putArgs, cancellationToken);

        return new StoredFile
        {
            Id = Guid.NewGuid().ToString(),
            FileName = request.FileName,
            ContentType = request.ContentType,
            Size = request.Stream.Length,
            StoragePath = objectName,
            Container = bucket,
            UploadedAt = DateTime.UtcNow,
            Metadata = request.Metadata,
            ETag = result.Etag
        };
    }

    public async Task<FileDownloadResult> DownloadAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var bucket = container ?? _settings.DefaultBucket;
        var metadata = await GetMetadataAsync(storagePath, bucket, cancellationToken);

        if (metadata == null)
        {
            throw new FileNotFoundException($"File not found: {storagePath}");
        }

        var stream = new MemoryStream();
        var getArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(storagePath)
            .WithCallbackStream(async (s, ct) =>
            {
                await s.CopyToAsync(stream, ct);
                stream.Position = 0;
            });

        await _client.GetObjectAsync(getArgs, cancellationToken);

        return new FileDownloadResult
        {
            File = metadata,
            Stream = stream
        };
    }

    public async Task<StoredFile?> GetMetadataAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var bucket = container ?? _settings.DefaultBucket;

        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(storagePath);

            var stat = await _client.StatObjectAsync(statArgs, cancellationToken);

            return new StoredFile
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(storagePath),
                ContentType = stat.ContentType,
                Size = stat.Size,
                StoragePath = storagePath,
                Container = bucket,
                UploadedAt = stat.LastModified,
                Metadata = stat.MetaData,
                ETag = stat.ETag
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var bucket = container ?? _settings.DefaultBucket;

        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(storagePath);

            await _client.RemoveObjectAsync(removeArgs, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(storagePath, container, cancellationToken);
        return metadata != null;
    }

    public async Task<IReadOnlyList<StoredFile>> ListAsync(string? prefix = null, string? container = null, CancellationToken cancellationToken = default)
    {
        var bucket = container ?? _settings.DefaultBucket;
        var result = new List<StoredFile>();

        var listArgs = new ListObjectsArgs()
            .WithBucket(bucket)
            .WithRecursive(true);

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            listArgs.WithPrefix(prefix);
        }

        var observable = _client.ListObjectsAsync(listArgs, cancellationToken);

        await foreach (var item in observable.WithCancellation(cancellationToken))
        {
            if (item.IsDir)
            {
                continue;
            }

            result.Add(new StoredFile
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(item.Key),
                ContentType = "application/octet-stream",
                Size = (long)item.Size,
                StoragePath = item.Key,
                Container = bucket,
                UploadedAt = item.LastModifiedDateTime ?? DateTime.UtcNow,
                ETag = item.ETag
            });
        }

        return result;
    }

    public async Task<string> GetPresignedUrlAsync(string storagePath, string? container = null, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default)
    {
        var bucket = container ?? _settings.DefaultBucket;
        var expiry = (int)(expiresIn ?? TimeSpan.FromHours(1)).TotalSeconds;

        var presignedArgs = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(storagePath)
            .WithExpiry(expiry);

        return await _client.PresignedGetObjectAsync(presignedArgs);
    }

    public async Task<StoredFile> CopyAsync(string sourceStoragePath, string destinationStoragePath, string? sourceContainer = null, string? destinationContainer = null, CancellationToken cancellationToken = default)
    {
        var sourceBucket = sourceContainer ?? _settings.DefaultBucket;
        var destBucket = destinationContainer ?? _settings.DefaultBucket;

        await EnsureBucketExistsAsync(destBucket, cancellationToken);

        var copyArgs = new CopyObjectArgs()
            .WithBucket(destBucket)
            .WithObject(destinationStoragePath)
            .WithCopyObjectSource(new CopySourceObjectArgs()
                .WithBucket(sourceBucket)
                .WithObject(sourceStoragePath));

        await _client.CopyObjectAsync(copyArgs, cancellationToken);

        var metadata = await GetMetadataAsync(destinationStoragePath, destBucket, cancellationToken);
        if (metadata == null)
        {
            throw new InvalidOperationException($"Failed to copy file to: {destinationStoragePath}");
        }

        return metadata;
    }

    public async Task<StoredFile> MoveAsync(string sourceStoragePath, string destinationStoragePath, string? sourceContainer = null, string? destinationContainer = null, CancellationToken cancellationToken = default)
    {
        var copiedFile = await CopyAsync(sourceStoragePath, destinationStoragePath, sourceContainer, destinationContainer, cancellationToken);
        await DeleteAsync(sourceStoragePath, sourceContainer, cancellationToken);
        return copiedFile;
    }

    private async Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken)
    {
        var bucketArgs = new BucketExistsArgs()
            .WithBucket(bucket);

        var exists = await _client.BucketExistsAsync(bucketArgs, cancellationToken);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(bucket);

            await _client.MakeBucketAsync(makeBucketArgs, cancellationToken);
        }
    }

    private string GenerateStoragePath(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.NewGuid().ToString("N");
        return $"{timestamp}/{guid}{extension}";
    }
}

/// <summary>
/// Settings for MinIO/S3-compatible storage.
/// </summary>
public class MinioFileStorageSettings
{
    /// <summary>
    /// MinIO/S3 endpoint (e.g., "play.min.io" or "s3.amazonaws.com").
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Access key for authentication.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret key for authentication.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS.
    /// </summary>
    public bool UseSSL { get; set; } = true;

    /// <summary>
    /// Region (optional).
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Default bucket name.
    /// </summary>
    public string DefaultBucket { get; set; } = "files";
}
