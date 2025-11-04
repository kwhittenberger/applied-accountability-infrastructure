using AppliedAccountability.Notifications.Abstractions;
using AppliedAccountability.Notifications.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AppliedAccountability.Notifications.Providers;

/// <summary>
/// Email provider using SMTP.
/// </summary>
public class SmtpEmailProvider : INotificationProvider
{
    private readonly SmtpSettings _settings;
    private readonly ITemplateService? _templateService;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public NotificationChannel Channel => NotificationChannel.Email;

    public SmtpEmailProvider(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailProvider> logger,
        ITemplateService? templateService = null)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
        _templateService = templateService;
    }

    public async Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await CreateMessageAsync(request, cancellationToken);

            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            // Send message
            var response = await client.SendAsync(message, cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {Recipient}. Message ID: {MessageId}",
                request.To,
                message.MessageId);

            return NotificationResult.Success(request.Id, Channel, message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", request.To);
            return NotificationResult.Failure(request.Id, Channel, ex.Message);
        }
    }

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

    private async Task<MimeMessage> CreateMessageAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();

        // From
        var fromAddress = !string.IsNullOrEmpty(request.From)
            ? request.From
            : _settings.DefaultFromAddress;

        var fromName = _settings.DefaultFromName ?? fromAddress;
        message.From.Add(new MailboxAddress(fromName, fromAddress));

        // To
        message.To.Add(MailboxAddress.Parse(request.To));

        // Subject
        message.Subject = request.Subject ?? "Notification";

        // Body
        string body;
        if (!string.IsNullOrEmpty(request.TemplateName) && _templateService != null)
        {
            body = await _templateService.RenderTemplateAsync(
                request.TemplateName,
                request.TemplateData,
                cancellationToken);
        }
        else if (request.TemplateData != null && _templateService != null)
        {
            body = await _templateService.RenderAsync(
                request.Body,
                request.TemplateData,
                cancellationToken);
        }
        else
        {
            body = request.Body;
        }

        // Check if HTML
        if (body.Trim().StartsWith("<") && body.Contains("</"))
        {
            message.Body = new TextPart("html") { Text = body };
        }
        else
        {
            message.Body = new TextPart("plain") { Text = body };
        }

        return message;
    }
}

/// <summary>
/// SMTP settings configuration.
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server hostname.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// SMTP server port (default: 587).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Use SSL/TLS.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// SMTP username (optional).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SMTP password (optional).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Default from email address.
    /// </summary>
    public required string DefaultFromAddress { get; set; }

    /// <summary>
    /// Default from name.
    /// </summary>
    public string? DefaultFromName { get; set; }
}
