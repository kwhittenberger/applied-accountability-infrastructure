using AppliedAccountability.Files.Models;

namespace AppliedAccountability.Files.Abstractions;

/// <summary>
/// Interface for file storage operations.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads a file to storage.
    /// </summary>
    Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    Task<FileDownloadResult> DownloadAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata without downloading the file.
    /// </summary>
    Task<StoredFile?> GetMetadataAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task<bool> DeleteAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(string storagePath, string? container = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a container/directory.
    /// </summary>
    Task<IReadOnlyList<StoredFile>> ListAsync(string? prefix = null, string? container = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed URL for temporary access to a file.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string storagePath, string? container = null, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file within storage.
    /// </summary>
    Task<StoredFile> CopyAsync(string sourceStoragePath, string destinationStoragePath, string? sourceContainer = null, string? destinationContainer = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file within storage.
    /// </summary>
    Task<StoredFile> MoveAsync(string sourceStoragePath, string destinationStoragePath, string? sourceContainer = null, string? destinationContainer = null, CancellationToken cancellationToken = default);
}
