# AppliedAccountability.Files

A unified file storage abstraction library that provides consistent API for multiple storage backends including local file system and MinIO/S3-compatible storage.

## Features

- **Unified Interface**: Single `IFileStorage` interface for all storage providers
- **Multiple Providers**: Support for local file system and MinIO/S3-compatible storage
- **Metadata Management**: Automatic metadata tracking (filename, content type, size, etc.)
- **Pre-signed URLs**: Generate temporary access URLs for files
- **File Operations**: Upload, download, copy, move, delete, and list files
- **Container/Bucket Support**: Organize files into logical containers
- **Auto-generated Paths**: Automatic storage path generation with timestamps
- **ETag Support**: Version tracking with ETags

## Installation

```bash
dotnet add package AppliedAccountability.Files
```

## Storage Providers

### Local File Storage

Stores files on the local file system with metadata tracking.

**Features:**
- Files stored in configurable root directory
- JSON metadata files for each uploaded file
- Automatic directory creation
- ETag calculation using MD5
- Container support via subdirectories

**Configuration:**

```json
{
  "LocalFileStorage": {
    "RootPath": "/var/files",
    "DefaultContainer": "uploads"
  }
}
```

**Registration:**

```csharp
// From configuration
services.AddLocalFileStorage(configuration);

// Or with explicit settings
services.AddLocalFileStorage(settings =>
{
    settings.RootPath = "/var/files";
    settings.DefaultContainer = "uploads";
});
```

### MinIO/S3-Compatible Storage

Stores files in MinIO or any S3-compatible storage service.

**Features:**
- Compatible with MinIO, AWS S3, DigitalOcean Spaces, etc.
- Pre-signed URL generation
- Automatic bucket creation
- Native metadata support
- Optional SSL/TLS

**Configuration:**

```json
{
  "MinIO": {
    "Endpoint": "play.min.io",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "UseSSL": true,
    "Region": "us-east-1",
    "DefaultBucket": "files"
  }
}
```

**Registration:**

```csharp
// From configuration
services.AddMinioFileStorage(configuration);

// Or with explicit settings
services.AddMinioFileStorage(settings =>
{
    settings.Endpoint = "play.min.io";
    settings.AccessKey = "your-access-key";
    settings.SecretKey = "your-secret-key";
    settings.UseSSL = true;
    settings.DefaultBucket = "files";
});
```

## Usage Examples

### 1. Uploading Files

```csharp
public class FileUploadService
{
    private readonly IFileStorage _storage;

    public FileUploadService(IFileStorage storage)
    {
        _storage = storage;
    }

    public async Task<StoredFile> UploadUserAvatarAsync(Stream fileStream, string fileName)
    {
        var request = new FileUploadRequest
        {
            Stream = fileStream,
            FileName = fileName,
            ContentType = "image/jpeg",
            Container = "avatars",
            Metadata = new Dictionary<string, string>
            {
                ["UploadedBy"] = "user123",
                ["Purpose"] = "avatar"
            }
        };

        return await _storage.UploadAsync(request);
    }
}
```

### 2. Downloading Files

```csharp
public async Task<Stream> DownloadFileAsync(string storagePath)
{
    var result = await _storage.DownloadAsync(storagePath, container: "avatars");

    // result.File contains metadata
    Console.WriteLine($"Downloading: {result.File.FileName}");
    Console.WriteLine($"Size: {result.File.Size} bytes");

    return result.Stream;
}
```

### 3. Getting File Metadata

```csharp
public async Task<StoredFile?> GetFileInfoAsync(string storagePath)
{
    var metadata = await _storage.GetMetadataAsync(storagePath, container: "avatars");

    if (metadata != null)
    {
        Console.WriteLine($"File: {metadata.FileName}");
        Console.WriteLine($"Content-Type: {metadata.ContentType}");
        Console.WriteLine($"Size: {metadata.Size} bytes");
        Console.WriteLine($"Uploaded: {metadata.UploadedAt}");
        Console.WriteLine($"ETag: {metadata.ETag}");
    }

    return metadata;
}
```

### 4. Generating Pre-signed URLs

```csharp
public async Task<string> GetTemporaryDownloadLinkAsync(string storagePath)
{
    // Generate URL valid for 2 hours
    var url = await _storage.GetPresignedUrlAsync(
        storagePath,
        container: "avatars",
        expiresIn: TimeSpan.FromHours(2)
    );

    return url;
}
```

### 5. Listing Files

```csharp
public async Task<IReadOnlyList<StoredFile>> ListUserFilesAsync(string userId)
{
    // List all files with prefix matching the user ID
    var files = await _storage.ListAsync(
        prefix: $"users/{userId}/",
        container: "documents"
    );

    foreach (var file in files)
    {
        Console.WriteLine($"{file.FileName} - {file.Size} bytes");
    }

    return files;
}
```

### 6. Copying and Moving Files

```csharp
public async Task<StoredFile> ArchiveFileAsync(string storagePath)
{
    // Copy file to archive container
    var archivedFile = await _storage.CopyAsync(
        sourceStoragePath: storagePath,
        destinationStoragePath: $"archived/{storagePath}",
        sourceContainer: "documents",
        destinationContainer: "archives"
    );

    return archivedFile;
}

public async Task<StoredFile> MoveToProcessedAsync(string storagePath)
{
    // Move file (copy then delete)
    var movedFile = await _storage.MoveAsync(
        sourceStoragePath: storagePath,
        destinationStoragePath: $"processed/{storagePath}",
        sourceContainer: "incoming",
        destinationContainer: "processed"
    );

    return movedFile;
}
```

### 7. Deleting Files

```csharp
public async Task<bool> DeleteFileAsync(string storagePath)
{
    var deleted = await _storage.DeleteAsync(storagePath, container: "temporary");

    if (deleted)
    {
        Console.WriteLine("File deleted successfully");
    }

    return deleted;
}
```

### 8. Checking File Existence

```csharp
public async Task<bool> CheckFileExistsAsync(string storagePath)
{
    var exists = await _storage.ExistsAsync(storagePath, container: "documents");
    return exists;
}
```

## Complete Example: Document Upload API

```csharp
public class DocumentController : ControllerBase
{
    private readonly IFileStorage _storage;

    public DocumentController(IFileStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        await using var stream = file.OpenReadStream();

        var request = new FileUploadRequest
        {
            Stream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Container = "documents",
            Metadata = new Dictionary<string, string>
            {
                ["OriginalName"] = file.FileName,
                ["UploadedBy"] = User.Identity?.Name ?? "Anonymous",
                ["UploadDate"] = DateTime.UtcNow.ToString("O")
            }
        };

        var storedFile = await _storage.UploadAsync(request);

        return Ok(new
        {
            id = storedFile.Id,
            fileName = storedFile.FileName,
            size = storedFile.Size,
            storagePath = storedFile.StoragePath,
            uploadedAt = storedFile.UploadedAt
        });
    }

    [HttpGet("download/{*storagePath}")]
    public async Task<IActionResult> DownloadDocument(string storagePath)
    {
        var exists = await _storage.ExistsAsync(storagePath, container: "documents");
        if (!exists)
        {
            return NotFound();
        }

        var result = await _storage.DownloadAsync(storagePath, container: "documents");

        return File(result.Stream, result.File.ContentType, result.File.FileName);
    }

    [HttpGet("temporary-link/{*storagePath}")]
    public async Task<IActionResult> GetTemporaryLink(string storagePath)
    {
        var url = await _storage.GetPresignedUrlAsync(
            storagePath,
            container: "documents",
            expiresIn: TimeSpan.FromMinutes(30)
        );

        return Ok(new { url, expiresIn = "30 minutes" });
    }

    [HttpDelete("{*storagePath}")]
    public async Task<IActionResult> DeleteDocument(string storagePath)
    {
        var deleted = await _storage.DeleteAsync(storagePath, container: "documents");

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
```

## Best Practices

### 1. Stream Management

Always dispose of streams properly:

```csharp
// Download
await using var result = await _storage.DownloadAsync(path);
// Stream is automatically disposed

// Upload
await using var fileStream = File.OpenRead(filePath);
await _storage.UploadAsync(new FileUploadRequest
{
    Stream = fileStream,
    FileName = fileName,
    ContentType = contentType
});
```

### 2. Storage Path Organization

Use logical path hierarchies:

```csharp
// Good: Organized by date and user
var storagePath = $"users/{userId}/documents/{year}/{month}/{fileName}";

// Auto-generated paths use: yyyy/MM/dd/{guid}{extension}
var request = new FileUploadRequest
{
    Stream = stream,
    FileName = fileName,
    ContentType = contentType
    // StoragePath omitted - will auto-generate
};
```

### 3. Metadata Usage

Store important context in metadata:

```csharp
var request = new FileUploadRequest
{
    Stream = stream,
    FileName = fileName,
    ContentType = contentType,
    Metadata = new Dictionary<string, string>
    {
        ["UserId"] = userId,
        ["DocumentType"] = "Invoice",
        ["FiscalYear"] = "2025",
        ["Department"] = "Accounting",
        ["ProcessedBy"] = currentUser
    }
};
```

### 4. Container/Bucket Organization

Separate files by purpose:

```csharp
// User-uploaded files
await _storage.UploadAsync(request with { Container = "uploads" });

// Generated reports
await _storage.UploadAsync(request with { Container = "reports" });

// Temporary files
await _storage.UploadAsync(request with { Container = "temp" });

// Archives
await _storage.UploadAsync(request with { Container = "archives" });
```

### 5. Pre-signed URL Security

Use short expiration times for sensitive files:

```csharp
// Public files: longer expiration
var publicUrl = await _storage.GetPresignedUrlAsync(
    path,
    expiresIn: TimeSpan.FromDays(7)
);

// Sensitive files: short expiration
var sensitiveUrl = await _storage.GetPresignedUrlAsync(
    path,
    expiresIn: TimeSpan.FromMinutes(5)
);
```

## Switching Between Providers

The abstraction makes it easy to switch storage providers without changing application code:

```csharp
// Development: Local storage
if (env.IsDevelopment())
{
    services.AddLocalFileStorage(settings =>
    {
        settings.RootPath = Path.Combine(env.ContentRootPath, "uploads");
    });
}
// Production: MinIO/S3
else
{
    services.AddMinioFileStorage(configuration);
}
```

## Architecture Notes

- **Provider Pattern**: Easily extend with new storage providers by implementing `IFileStorage`
- **Cloud-Agnostic**: No hard dependencies on specific cloud providers
- **Self-Hosted First**: Local and MinIO providers support self-hosted deployments
- **Metadata Persistence**: Local provider stores metadata as JSON; MinIO uses native metadata
- **Auto-Path Generation**: Providers generate organized paths using date hierarchies
- **Stream-Based**: Efficient memory usage with stream-based operations

## Related Libraries

- **AppliedAccountability.Data**: Data access patterns
- **AppliedAccountability.EventStore**: Event sourcing
- **AppliedAccountability.Notifications**: Multi-channel notifications

## License

MIT
