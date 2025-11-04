namespace AppliedAccountability.Notifications.Models;

/// <summary>
/// Notification priority levels.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority - can be batched and delayed.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard delivery.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - expedited delivery.
    /// </summary>
    High = 2,

    /// <summary>
    /// Urgent priority - immediate delivery.
    /// </summary>
    Urgent = 3
}
