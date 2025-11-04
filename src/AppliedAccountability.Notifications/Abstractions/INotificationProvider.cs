using AppliedAccountability.Notifications.Models;

namespace AppliedAccountability.Notifications.Abstractions;

/// <summary>
/// Interface for notification delivery providers.
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// Channel supported by this provider.
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Sends a notification.
    /// </summary>
    Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple notifications in batch.
    /// </summary>
    Task<IReadOnlyList<NotificationResult>> SendBatchAsync(
        IEnumerable<NotificationRequest> requests,
        CancellationToken cancellationToken = default);
}
