namespace AppliedAccountability.Files.Models;

/// <summary>
/// Represents metadata for a stored file.
/// </summary>
public class StoredFile
{
    /// <summary>
    /// Unique identifier for the file.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Content type (MIME type).
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Storage path or key.
    /// </summary>
    public required string StoragePath { get; set; }

    /// <summary>
    /// Container or bucket name.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Who uploaded the file.
    /// </summary>
    public string? UploadedBy { get; set; }

    /// <summary>
    /// Optional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// ETag for version tracking.
    /// </summary>
    public string? ETag { get; set; }
}
