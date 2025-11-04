namespace AppliedAccountability.Notifications.Models;

/// <summary>
/// Represents a notification to be sent.
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Unique identifier for the notification (optional, will be generated if not provided).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Notification channel to use.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Recipient address (email, phone number, device token, etc.).
    /// </summary>
    public required string To { get; set; }

    /// <summary>
    /// Sender address (optional, uses default if not specified).
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Notification subject (for email, push title).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Notification body/message.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Template name to use (optional).
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Template data for variable substitution.
    /// </summary>
    public Dictionary<string, object>? TemplateData { get; set; }

    /// <summary>
    /// Priority level.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Schedule delivery for a specific time (optional).
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Correlation ID for tracking related notifications.
    /// </summary>
    public string? CorrelationId { get; set; }
}
