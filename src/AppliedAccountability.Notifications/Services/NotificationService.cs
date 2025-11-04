using AppliedAccountability.Notifications.Abstractions;
using AppliedAccountability.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.Notifications.Services;

/// <summary>
/// Main notification service that coordinates multiple providers.
/// </summary>
public class NotificationService
{
    private readonly Dictionary<NotificationChannel, INotificationProvider> _providers = new();
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEnumerable<INotificationProvider> providers,
        ILogger<NotificationService> logger)
    {
        _logger = logger;

        foreach (var provider in providers)
        {
            _providers[provider.Channel] = provider;
        }
    }

    /// <summary>
    /// Sends a notification through the specified channel.
    /// </summary>
    public async Task<NotificationResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(request.Channel, out var provider))
        {
            _logger.LogError("No provider registered for channel {Channel}", request.Channel);
            return NotificationResult.Failure(
                request.Id,
                request.Channel,
                $"No provider registered for channel {request.Channel}");
        }

        _logger.LogInformation(
            "Sending {Channel} notification {NotificationId} to {Recipient}",
            request.Channel,
            request.Id,
            request.To);

        return await provider.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends multiple notifications.
    /// </summary>
    public async Task<IReadOnlyList<NotificationResult>> SendBatchAsync(
        IEnumerable<NotificationRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationResult>();

        foreach (var request in requests)
        {
            var result = await SendAsync(request, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Checks if a channel is available.
    /// </summary>
    public bool IsChannelAvailable(NotificationChannel channel)
    {
        return _providers.ContainsKey(channel);
    }
}
