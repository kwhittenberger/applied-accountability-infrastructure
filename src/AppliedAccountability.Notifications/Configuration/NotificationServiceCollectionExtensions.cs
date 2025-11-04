using AppliedAccountability.Notifications.Abstractions;
using AppliedAccountability.Notifications.Providers;
using AppliedAccountability.Notifications.Services;
using AppliedAccountability.Notifications.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppliedAccountability.Notifications.Configuration;

/// <summary>
/// Extension methods for registering notification services.
/// </summary>
public static class NotificationServiceCollectionExtensions
{
    /// <summary>
    /// Adds notification services with SMTP email support.
    /// </summary>
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register template service
        services.AddSingleton<ITemplateService, TemplateService>();

        // Register notification service
        services.AddScoped<NotificationService>();

        return services;
    }

    /// <summary>
    /// Adds SMTP email provider.
    /// </summary>
    public static IServiceCollection AddSmtpEmail(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "Smtp")
    {
        services.Configure<SmtpSettings>(configuration.GetSection(configSectionName));
        services.AddScoped<INotificationProvider, SmtpEmailProvider>();

        return services;
    }

    /// <summary>
    /// Adds SMTP email provider with custom configuration.
    /// </summary>
    public static IServiceCollection AddSmtpEmail(
        this IServiceCollection services,
        Action<SmtpSettings> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddScoped<INotificationProvider, SmtpEmailProvider>();

        return services;
    }
}
