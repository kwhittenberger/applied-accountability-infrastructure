using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppliedAccountability.Files.Abstractions;
using AppliedAccountability.Files.Models;

namespace AppliedAccountability.Files.Providers;

/// <summary>
/// Local file system storage implementation.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly LocalFileStorageSettings _settings;

    public LocalFileStorage(LocalFileStorageSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrWhiteSpace(_settings.RootPath))
        {
            throw new ArgumentException("RootPath is required", nameof(settings));
        }

        Directory.CreateDirectory(_settings.RootPath);
    }

    public async Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        var container = request.Container ?? _settings.DefaultContainer;
        var storagePath = request.StoragePath ?? GenerateStoragePath(request.FileName);

        var fullPath = GetFullPath(storagePath, container);
        var directory = Path.GetDirectoryName(fullPath);

        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        if (!request.Overwrite && File.Exists(fullPath))
        {
            throw new InvalidOperationException($"File already exists at path: {storagePath}");
        }

        var fileInfo = new FileInfo(fullPath);
        await using (var fileStream = fileInfo.Create())
        {
            await request.Stream.CopyToAsync(fileStream, cancellationToken);
        }

        var storedFile = new StoredFile
        {
            Id = Guid.NewGuid().ToString(),
            FileName = request.FileName,
            ContentType = request.ContentType,
            Size = fileInfo.Length,
            StoragePath = storagePath,
            Container = container,
            UploadedAt = DateTime.UtcNow,
            Metadata = request.Metadata,
            ETag = await CalculateETagAsync(fullPath, cancellationToken)
        };

        await SaveMetadataAsync(fullPath, storedFile, cancellationToken);

        return storedFile;
    }

    public async Task<FileDownloadResult> DownloadAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath, container ?? _settings.DefaultContainer);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {storagePath}");
        }

        var metadata = await GetMetadataAsync(storagePath, container, cancellationToken);
        if (metadata == null)
        {
            throw new FileNotFoundException($"File metadata not found: {storagePath}");
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return new FileDownloadResult
        {
            File = metadata,
            Stream = stream
        };
    }

    public async Task<StoredFile?> GetMetadataAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath, container ?? _settings.DefaultContainer);
        var metadataPath = GetMetadataPath(fullPath);

        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
        return JsonSerializer.Deserialize<StoredFile>(json);
    }

    public Task<bool> DeleteAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath, container ?? _settings.DefaultContainer);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);

        var metadataPath = GetMetadataPath(fullPath);
        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath, container ?? _settings.DefaultContainer);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<IReadOnlyList<StoredFile>> ListAsync(string? prefix = null, string? container = null, CancellationToken cancellationToken = default)
    {
        var containerPath = GetContainerPath(container ?? _settings.DefaultContainer);

        if (!Directory.Exists(containerPath))
        {
            return Array.Empty<StoredFile>();
        }

        var searchPattern = string.IsNullOrWhiteSpace(prefix) ? "*" : $"{prefix}*";
        var files = Directory.GetFiles(containerPath, searchPattern, SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".metadata.json"));

        var result = new List<StoredFile>();
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(containerPath, file);
            var metadata = await GetMetadataAsync(relativePath, container, cancellationToken);
            if (metadata != null)
            {
                result.Add(metadata);
            }
        }

        return result;
    }

    public Task<string> GetPresignedUrlAsync(string storagePath, string? container = null, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath, container ?? _settings.DefaultContainer);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {storagePath}");
        }

        // For local storage, return file:// URL
        return Task.FromResult($"file://{fullPath}");
    }

    public async Task<StoredFile> CopyAsync(string sourceStoragePath, string destinationStoragePath, string? sourceContainer = null, string? destinationContainer = null, CancellationToken cancellationToken = default)
    {
        var sourcePath = GetFullPath(sourceStoragePath, sourceContainer ?? _settings.DefaultContainer);
        var destPath = GetFullPath(destinationStoragePath, destinationContainer ?? _settings.DefaultContainer);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourceStoragePath}");
        }

        var destDirectory = Path.GetDirectoryName(destPath);
        if (destDirectory != null)
        {
            Directory.CreateDirectory(destDirectory);
        }

        File.Copy(sourcePath, destPath, true);

        var sourceMetadata = await GetMetadataAsync(sourceStoragePath, sourceContainer, cancellationToken);
        if (sourceMetadata == null)
        {
            throw new InvalidOperationException($"Source metadata not found: {sourceStoragePath}");
        }

        var copiedFile = new StoredFile
        {
            Id = Guid.NewGuid().ToString(),
            FileName = sourceMetadata.FileName,
            ContentType = sourceMetadata.ContentType,
            Size = sourceMetadata.Size,
            StoragePath = destinationStoragePath,
            Container = destinationContainer ?? _settings.DefaultContainer,
            UploadedAt = DateTime.UtcNow,
            Metadata = sourceMetadata.Metadata,
            ETag = await CalculateETagAsync(destPath, cancellationToken)
        };

        await SaveMetadataAsync(destPath, copiedFile, cancellationToken);

        return copiedFile;
    }

    public async Task<StoredFile> MoveAsync(string sourceStoragePath, string destinationStoragePath, string? sourceContainer = null, string? destinationContainer = null, CancellationToken cancellationToken = default)
    {
        var copiedFile = await CopyAsync(sourceStoragePath, destinationStoragePath, sourceContainer, destinationContainer, cancellationToken);
        await DeleteAsync(sourceStoragePath, sourceContainer, cancellationToken);
        return copiedFile;
    }

    private string GetFullPath(string storagePath, string container)
    {
        var containerPath = GetContainerPath(container);
        return Path.Combine(containerPath, storagePath);
    }

    private string GetContainerPath(string container)
    {
        return Path.Combine(_settings.RootPath, container);
    }

    private string GetMetadataPath(string fullPath)
    {
        return $"{fullPath}.metadata.json";
    }

    private string GenerateStoragePath(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.NewGuid().ToString("N");
        return $"{timestamp}/{guid}{extension}";
    }

    private async Task<string> CalculateETagAsync(string filePath, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(filePath);
        var hash = await MD5.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task SaveMetadataAsync(string fullPath, StoredFile metadata, CancellationToken cancellationToken)
    {
        var metadataPath = GetMetadataPath(fullPath);
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }
}

/// <summary>
/// Settings for local file storage.
/// </summary>
public class LocalFileStorageSettings
{
    /// <summary>
    /// Root path for file storage.
    /// </summary>
    public required string RootPath { get; set; }

    /// <summary>
    /// Default container name.
    /// </summary>
    public string DefaultContainer { get; set; } = "files";
}
