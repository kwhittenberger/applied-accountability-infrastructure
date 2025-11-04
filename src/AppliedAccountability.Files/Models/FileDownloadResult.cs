namespace AppliedAccountability.Files.Models;

/// <summary>
/// Result of a file download operation.
/// </summary>
public class FileDownloadResult : IDisposable
{
    /// <summary>
    /// File metadata.
    /// </summary>
    public required StoredFile File { get; set; }

    /// <summary>
    /// File stream.
    /// </summary>
    public required Stream Stream { get; set; }

    public void Dispose()
    {
        Stream?.Dispose();
    }
}
