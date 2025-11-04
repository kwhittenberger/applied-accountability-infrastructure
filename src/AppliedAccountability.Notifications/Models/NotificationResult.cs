namespace AppliedAccountability.Notifications.Models;

/// <summary>
/// Result of a notification send operation.
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Notification ID.
    /// </summary>
    public required string NotificationId { get; set; }

    /// <summary>
    /// Whether the notification was sent successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Provider-specific message ID (for tracking).
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// Timestamp when sent.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Channel used.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Additional metadata from provider.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    public static NotificationResult Success(string notificationId, NotificationChannel channel, string? providerMessageId = null)
    {
        return new NotificationResult
        {
            NotificationId = notificationId,
            Channel = channel,
            IsSuccess = true,
            ProviderMessageId = providerMessageId
        };
    }

    public static NotificationResult Failure(string notificationId, NotificationChannel channel, string errorMessage)
    {
        return new NotificationResult
        {
            NotificationId = notificationId,
            Channel = channel,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
