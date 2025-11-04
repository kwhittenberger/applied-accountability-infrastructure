# AppliedAccountability.Notifications

Multi-channel notification system for email, SMS, push notifications, and webhooks with template support and delivery tracking.

## Features

- **Email Support** - SMTP with MailKit (HTML and plain text)
- **Template Engine** - Scriban-based templates with variable substitution
- **Multi-Channel** - Extensible architecture for Email, SMS, Push, Webhooks
- **Priority Levels** - Low, Normal, High, Urgent
- **Delivery Tracking** - Success/failure status with provider message IDs
- **Batch Sending** - Send multiple notifications efficiently
- **Type-Safe** - Strongly-typed models and interfaces

## Installation

```bash
dotnet add package AppliedAccountability.Notifications
```

## Quick Start

### 1. Configure Services

```csharp
using AppliedAccountability.Notifications.Configuration;

// In Program.cs
builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddSmtpEmail(builder.Configuration);
```

### 2. appsettings.json

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "DefaultFromAddress": "noreply@yourcompany.com",
    "DefaultFromName": "Your Company"
  }
}
```

## Usage Examples

### Example 1: Send Simple Email

```csharp
using AppliedAccountability.Notifications.Models;
using AppliedAccountability.Notifications.Services;

public class EmailService
{
    private readonly NotificationService _notificationService;

    public EmailService(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task SendWelcomeEmailAsync(string userEmail, string userName)
    {
        var notification = new NotificationRequest
        {
            Channel = NotificationChannel.Email,
            To = userEmail,
            Subject = "Welcome to Our Platform!",
            Body = $"Hello {userName},\n\nWelcome to our platform! We're excited to have you on board.",
            Priority = NotificationPriority.Normal
        };

        var result = await _notificationService.SendAsync(notification);

        if (result.IsSuccess)
        {
            Console.WriteLine($"Email sent successfully. Message ID: {result.ProviderMessageId}");
        }
        else
        {
            Console.WriteLine($"Failed to send email: {result.ErrorMessage}");
        }
    }
}
```

### Example 2: Send HTML Email

```csharp
public async Task SendHtmlEmailAsync(string recipientEmail)
{
    var notification = new NotificationRequest
    {
        Channel = NotificationChannel.Email,
        To = recipientEmail,
        Subject = "Your Monthly Report",
        Body = @"
            <html>
            <body>
                <h1>Monthly Report</h1>
                <p>Here's your activity summary for this month.</p>
                <ul>
                    <li>Total Orders: 25</li>
                    <li>Revenue: $1,250</li>
                </ul>
            </body>
            </html>
        "
    };

    await _notificationService.SendAsync(notification);
}
```

### Example 3: Using Templates

```csharp
public async Task SendTemplatedEmailAsync(string userEmail, string userName, string orderId)
{
    var notification = new NotificationRequest
    {
        Channel = NotificationChannel.Email,
        To = userEmail,
        Subject = "Order Confirmation",
        Body = @"
            Hello {{ user_name }},

            Thank you for your order!

            Order ID: {{ order_id }}
            Total: ${{ total }}

            Your order will be shipped within 2-3 business days.

            Best regards,
            The Team
        ",
        TemplateData = new Dictionary<string, object>
        {
            ["user_name"] = userName,
            ["order_id"] = orderId,
            ["total"] = "99.99"
        }
    };

    await _notificationService.SendAsync(notification);
}
```

### Example 4: Batch Sending

```csharp
public async Task SendBulkNotificationsAsync(List<string> emailAddresses)
{
    var requests = emailAddresses.Select(email => new NotificationRequest
    {
        Channel = NotificationChannel.Email,
        To = email,
        Subject = "Important Announcement",
        Body = "We have an important announcement to share with you...",
        Priority = NotificationPriority.Normal
    }).ToList();

    var results = await _notificationService.SendBatchAsync(requests);

    var successCount = results.Count(r => r.IsSuccess);
    var failureCount = results.Count(r => !r.IsSuccess);

    Console.WriteLine($"Sent: {successCount}, Failed: {failureCount}");
}
```

### Example 5: Priority and Scheduling

```csharp
public async Task SendUrgentAlertAsync(string adminEmail)
{
    var notification = new NotificationRequest
    {
        Channel = NotificationChannel.Email,
        To = adminEmail,
        Subject = "URGENT: System Alert",
        Body = "Critical system issue detected. Please investigate immediately.",
        Priority = NotificationPriority.Urgent,
        CorrelationId = Guid.NewGuid().ToString()
    };

    await _notificationService.SendAsync(notification);
}

public async Task SendScheduledReminderAsync(string userEmail, DateTime reminderTime)
{
    var notification = new NotificationRequest
    {
        Channel = NotificationChannel.Email,
        To = userEmail,
        Subject = "Reminder: Upcoming Appointment",
        Body = "This is a reminder about your appointment tomorrow.",
        ScheduledFor = reminderTime // For future enhancement with job scheduling
    };

    await _notificationService.SendAsync(notification);
}
```

### Example 6: Custom SMTP Configuration

```csharp
// In Program.cs - configure SMTP without appsettings.json
builder.Services.AddSmtpEmail(options =>
{
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.UseSsl = true;
    options.Username = "user@example.com";
    options.Password = "password";
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "Example App";
});
```

## Advanced Usage

### Named Templates

```csharp
using AppliedAccountability.Notifications.Abstractions;

public class TemplateManager
{
    private readonly ITemplateService _templateService;

    public TemplateManager(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    public void RegisterTemplates()
    {
        if (_templateService is Templates.TemplateService service)
        {
            service.RegisterTemplate("welcome_email", @"
                Hello {{ name }},

                Welcome to {{ company_name }}!

                Your account has been created successfully.
            ");

            service.RegisterTemplate("password_reset", @"
                Hi {{ name }},

                Click here to reset your password:
                {{ reset_link }}

                This link expires in 24 hours.
            ");
        }
    }
}
```

### Custom Provider

```csharp
using AppliedAccountability.Notifications.Abstractions;
using AppliedAccountability.Notifications.Models;

public class CustomSmsProvider : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task<NotificationResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Implement your SMS sending logic here
        // Example: call your SMS API

        return NotificationResult.Success(request.Id, Channel);
    }

    public async Task<IReadOnlyList<NotificationResult>> SendBatchAsync(
        IEnumerable<NotificationRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationResult>();
        foreach (var request in requests)
        {
            results.Add(await SendAsync(request, cancellationToken));
        }
        return results;
    }
}

// Register custom provider
builder.Services.AddScoped<INotificationProvider, CustomSmsProvider>();
```

## Configuration Options

### SmtpSettings

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| Host | string | SMTP server hostname | Required |
| Port | int | SMTP server port | 587 |
| UseSsl | bool | Use SSL/TLS | true |
| Username | string? | SMTP username | null |
| Password | string? | SMTP password | null |
| DefaultFromAddress | string | Default sender email | Required |
| DefaultFromName | string? | Default sender name | null |

## Notification Channels

- `Email` - Email notifications via SMTP
- `Sms` - SMS text messages (provider implementation required)
- `Push` - Push notifications (provider implementation required)
- `Webhook` - HTTP webhooks (provider implementation required)
- `InApp` - In-app notifications (provider implementation required)

## Priority Levels

- `Low` - Can be batched and delayed
- `Normal` - Standard delivery (default)
- `High` - Expedited delivery
- `Urgent` - Immediate delivery

## Integration with Conductor

For scheduled notifications, combine with Conductor:

```csharp
// In Conductor job
public class DailyDigestJob
{
    private readonly NotificationService _notificationService;

    public async Task ExecuteAsync()
    {
        var users = await GetActiveUsersAsync();

        foreach (var user in users)
        {
            await _notificationService.SendAsync(new NotificationRequest
            {
                Channel = NotificationChannel.Email,
                To = user.Email,
                Subject = "Your Daily Digest",
                Body = "Here's what happened today...",
                CorrelationId = $"daily-digest-{DateTime.UtcNow:yyyyMMdd}"
            });
        }
    }
}
```

## Requirements

- .NET 10.0 or later
- MailKit 4.8.0+ (for email)
- Scriban 5.10.0+ (for templates)

## License

MIT License - Copyright © Applied Accountability Services LLC 2025

## Contributing

This package is maintained by Applied Accountability Services LLC. For bug reports or feature requests, please open an issue on GitHub.
