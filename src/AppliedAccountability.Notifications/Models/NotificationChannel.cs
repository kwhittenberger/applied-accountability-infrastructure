namespace AppliedAccountability.Notifications.Models;

/// <summary>
/// Notification delivery channel types.
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Email notification.
    /// </summary>
    Email,

    /// <summary>
    /// SMS (text message) notification.
    /// </summary>
    Sms,

    /// <summary>
    /// Push notification to mobile devices.
    /// </summary>
    Push,

    /// <summary>
    /// Webhook HTTP callback.
    /// </summary>
    Webhook,

    /// <summary>
    /// In-app notification.
    /// </summary>
    InApp
}
