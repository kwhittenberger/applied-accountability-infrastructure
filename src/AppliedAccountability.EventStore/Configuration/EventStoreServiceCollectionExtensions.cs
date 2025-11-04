using AppliedAccountability.EventStore.Abstractions;
using AppliedAccountability.EventStore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppliedAccountability.EventStore.Configuration;

/// <summary>
/// Extension methods for registering EventStore services.
/// </summary>
public static class EventStoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds EventStore services to the dependency injection container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <param name="configSectionName">Configuration section name (default: "EventStore").</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "EventStore")
    {
        // Register options
        services.Configure<EventStoreOptions>(
            configuration.GetSection(configSectionName));

        var options = configuration.GetSection(configSectionName).Get<EventStoreOptions>();
        if (options == null || string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new InvalidOperationException(
                $"EventStore configuration section '{configSectionName}' is missing or invalid. " +
                "Please ensure 'ConnectionString' is configured.");
        }

        // Register DbContext
        services.AddDbContext<EventStoreDbContext>(opts =>
        {
            opts.UseNpgsql(
                options.ConnectionString,
                npgsqlOpts =>
                {
                    npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });

            if (options.EnableDetailedLogging)
            {
                opts.EnableSensitiveDataLogging();
                opts.EnableDetailedErrors();
            }
        });

        // Register event store services
        services.AddScoped<IEventStore, PostgresEventStore>();
        services.AddScoped<ISnapshotStore, PostgresSnapshotStore>();

        return services;
    }

    /// <summary>
    /// Adds EventStore services with a custom configuration action.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">Configuration action.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        Action<EventStoreOptions> configureOptions)
    {
        var options = new EventStoreOptions();
        configureOptions(options);

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "EventStore ConnectionString is required.");
        }

        // Register options
        services.Configure(configureOptions);

        // Register DbContext
        services.AddDbContext<EventStoreDbContext>(opts =>
        {
            opts.UseNpgsql(
                options.ConnectionString,
                npgsqlOpts =>
                {
                    npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });

            if (options.EnableDetailedLogging)
            {
                opts.EnableSensitiveDataLogging();
                opts.EnableDetailedErrors();
            }
        });

        // Register event store services
        services.AddScoped<IEventStore, PostgresEventStore>();
        services.AddScoped<ISnapshotStore, PostgresSnapshotStore>();

        return services;
    }
}
