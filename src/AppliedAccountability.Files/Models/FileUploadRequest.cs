namespace AppliedAccountability.Files.Models;

/// <summary>
/// Request for uploading a file.
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// File stream to upload.
    /// </summary>
    public required Stream Stream { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Content type (MIME type).
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Container or bucket name.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Optional custom storage path/key. If not provided, will be auto-generated.
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Optional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Whether to overwrite existing file.
    /// </summary>
    public bool Overwrite { get; set; } = false;
}
