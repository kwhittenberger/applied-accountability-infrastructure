# AppliedAccountability.EventStore - Design Document

## Overview

A lightweight event store library for audit logging, change tracking, and simple event sourcing patterns. This library focuses on **event persistence and retrieval** without the complexity of distributed workflows, message buses, or saga orchestration.

### When to Use EventStore

✅ **Use EventStore for:**
- Audit trails and compliance logging
- Change history tracking
- Domain event persistence
- Simple CQRS read model projections
- Point-in-time state reconstruction

❌ **Use Conductor instead for:**
- Distributed workflows and sagas
- Message bus integration
- Compensation transactions
- Multi-service coordination
- Job scheduling

---

## Core Concepts

### Event
An immutable record of something that happened in the system.

```csharp
public class Event
{
    public long Id { get; set; }                    // Auto-incrementing ID
    public string StreamId { get; set; }            // Aggregate ID (e.g., "user-123")
    public string EventType { get; set; }           // Event type (e.g., "UserCreated")
    public string EventData { get; set; }           // JSON payload
    public string? Metadata { get; set; }           // Optional metadata (user, IP, etc.)
    public int Version { get; set; }                // Event version in stream (starts at 1)
    public DateTime Timestamp { get; set; }         // When event occurred
    public Guid CorrelationId { get; set; }         // For tracing related events
}
```

### Stream
A sequence of events for a specific aggregate/entity (e.g., all events for "user-123").

### Snapshot
A point-in-time state checkpoint to avoid replaying all events.

```csharp
public class Snapshot
{
    public long Id { get; set; }
    public string StreamId { get; set; }            // Aggregate ID
    public int Version { get; set; }                // Last event version included
    public string StateData { get; set; }           // JSON state snapshot
    public DateTime Timestamp { get; set; }
}
```

---

## Database Schema

### events Table
```sql
CREATE TABLE events (
    id BIGSERIAL PRIMARY KEY,
    stream_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB,
    version INTEGER NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    correlation_id UUID NOT NULL,

    -- Ensure version uniqueness per stream
    CONSTRAINT unique_stream_version UNIQUE (stream_id, version)
);

-- Indexes for efficient queries
CREATE INDEX idx_events_stream_id ON events (stream_id);
CREATE INDEX idx_events_event_type ON events (event_type);
CREATE INDEX idx_events_timestamp ON events (timestamp);
CREATE INDEX idx_events_correlation_id ON events (correlation_id);
CREATE INDEX idx_events_stream_version ON events (stream_id, version);
```

### snapshots Table
```sql
CREATE TABLE snapshots (
    id BIGSERIAL PRIMARY KEY,
    stream_id VARCHAR(255) NOT NULL,
    version INTEGER NOT NULL,
    state_data JSONB NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT unique_stream_snapshot UNIQUE (stream_id, version)
);

CREATE INDEX idx_snapshots_stream_id ON snapshots (stream_id);
```

---

## API Design

### IEventStore Interface

```csharp
public interface IEventStore
{
    // Append events to a stream
    Task AppendAsync(
        string streamId,
        IEnumerable<EventData> events,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default);

    // Append a single event (convenience method)
    Task AppendAsync(
        string streamId,
        EventData eventData,
        CancellationToken cancellationToken = default);

    // Read all events from a stream
    Task<IReadOnlyList<Event>> ReadStreamAsync(
        string streamId,
        int fromVersion = 1,
        int? toVersion = null,
        CancellationToken cancellationToken = default);

    // Read events forward (chronological order)
    Task<IReadOnlyList<Event>> ReadForwardAsync(
        long fromPosition = 0,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    // Read events by type
    Task<IReadOnlyList<Event>> ReadEventTypeAsync(
        string eventType,
        DateTime? fromTimestamp = null,
        DateTime? toTimestamp = null,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    // Read events by correlation ID
    Task<IReadOnlyList<Event>> ReadByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
```

### ISnapshotStore Interface

```csharp
public interface ISnapshotStore
{
    // Save a snapshot
    Task SaveSnapshotAsync(
        string streamId,
        int version,
        object state,
        CancellationToken cancellationToken = default);

    // Get the latest snapshot for a stream
    Task<Snapshot?> GetLatestSnapshotAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    // Get a specific snapshot by version
    Task<Snapshot?> GetSnapshotAsync(
        string streamId,
        int version,
        CancellationToken cancellationToken = default);
}
```

### EventData (for appending)

```csharp
public class EventData
{
    public string EventType { get; set; }
    public object Data { get; set; }
    public object? Metadata { get; set; }
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
```

---

## Usage Examples

### Example 1: User Audit Trail

```csharp
public class UserService
{
    private readonly IEventStore _eventStore;

    public UserService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task CreateUserAsync(string userId, string email, string name)
    {
        // Create user in database...

        // Log event for audit trail
        await _eventStore.AppendAsync(
            streamId: $"user-{userId}",
            eventData: new EventData
            {
                EventType = "UserCreated",
                Data = new
                {
                    UserId = userId,
                    Email = email,
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                },
                Metadata = new
                {
                    PerformedBy = "system",
                    IpAddress = "127.0.0.1"
                }
            }
        );
    }

    public async Task UpdateEmailAsync(string userId, string newEmail)
    {
        // Update email in database...

        // Log event
        await _eventStore.AppendAsync(
            streamId: $"user-{userId}",
            eventData: new EventData
            {
                EventType = "UserEmailChanged",
                Data = new
                {
                    UserId = userId,
                    NewEmail = newEmail,
                    ChangedAt = DateTime.UtcNow
                }
            }
        );
    }

    // Get complete user history
    public async Task<IReadOnlyList<Event>> GetUserHistoryAsync(string userId)
    {
        return await _eventStore.ReadStreamAsync($"user-{userId}");
    }
}
```

### Example 2: Event Sourcing with State Reconstruction

```csharp
public class Account
{
    public string AccountId { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }

    // Apply events to rebuild state
    public void Apply(Event @event)
    {
        switch (@event.EventType)
        {
            case "AccountOpened":
                var opened = JsonSerializer.Deserialize<AccountOpenedEvent>(@event.EventData);
                AccountId = opened.AccountId;
                Balance = opened.InitialDeposit;
                IsActive = true;
                break;

            case "MoneyDeposited":
                var deposited = JsonSerializer.Deserialize<MoneyDepositedEvent>(@event.EventData);
                Balance += deposited.Amount;
                break;

            case "MoneyWithdrawn":
                var withdrawn = JsonSerializer.Deserialize<MoneyWithdrawnEvent>(@event.EventData);
                Balance -= withdrawn.Amount;
                break;

            case "AccountClosed":
                IsActive = false;
                break;
        }
    }
}

public class AccountService
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;

    public async Task<Account> GetAccountAsync(string accountId)
    {
        var account = new Account();
        var streamId = $"account-{accountId}";

        // Try to load from snapshot first
        var snapshot = await _snapshotStore.GetLatestSnapshotAsync(streamId);
        int fromVersion = 1;

        if (snapshot != null)
        {
            account = JsonSerializer.Deserialize<Account>(snapshot.StateData);
            fromVersion = snapshot.Version + 1;
        }

        // Load events since snapshot
        var events = await _eventStore.ReadStreamAsync(streamId, fromVersion);

        // Replay events to rebuild state
        foreach (var @event in events)
        {
            account.Apply(@event);
        }

        return account;
    }

    public async Task DepositAsync(string accountId, decimal amount)
    {
        var streamId = $"account-{accountId}";

        await _eventStore.AppendAsync(
            streamId,
            new EventData
            {
                EventType = "MoneyDeposited",
                Data = new MoneyDepositedEvent
                {
                    AccountId = accountId,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                }
            }
        );

        // Create snapshot every 10 events
        var events = await _eventStore.ReadStreamAsync(streamId);
        if (events.Count % 10 == 0)
        {
            var account = await GetAccountAsync(accountId);
            await _snapshotStore.SaveSnapshotAsync(
                streamId,
                events.Count,
                account
            );
        }
    }
}
```

### Example 3: Compliance Audit Report

```csharp
public class ComplianceService
{
    private readonly IEventStore _eventStore;

    public async Task<AuditReport> GenerateUserActivityReportAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate)
    {
        var events = await _eventStore.ReadStreamAsync($"user-{userId}");

        var activities = events
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .Select(e => new AuditEntry
            {
                Timestamp = e.Timestamp,
                Action = e.EventType,
                Details = e.EventData,
                CorrelationId = e.CorrelationId
            })
            .ToList();

        return new AuditReport
        {
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            TotalActivities = activities.Count,
            Activities = activities
        };
    }

    public async Task<List<string>> FindUsersByActionAsync(
        string eventType,
        DateTime fromDate)
    {
        var events = await _eventStore.ReadEventTypeAsync(
            eventType,
            fromTimestamp: fromDate
        );

        return events
            .Select(e => JsonSerializer.Deserialize<dynamic>(e.EventData))
            .Select(data => (string)data.UserId)
            .Distinct()
            .ToList();
    }
}
```

### Example 4: Correlation ID Tracking

```csharp
public class OrderService
{
    private readonly IEventStore _eventStore;

    public async Task ProcessOrderAsync(string orderId)
    {
        var correlationId = Guid.NewGuid();

        // All related events share the same correlation ID
        await _eventStore.AppendAsync($"order-{orderId}", new EventData
        {
            EventType = "OrderReceived",
            Data = new { OrderId = orderId },
            CorrelationId = correlationId
        });

        // ... process order ...

        await _eventStore.AppendAsync($"inventory-{orderId}", new EventData
        {
            EventType = "InventoryReserved",
            Data = new { OrderId = orderId },
            CorrelationId = correlationId
        });

        await _eventStore.AppendAsync($"payment-{orderId}", new EventData
        {
            EventType = "PaymentProcessed",
            Data = new { OrderId = orderId },
            CorrelationId = correlationId
        });
    }

    // Get all events related to a transaction
    public async Task<IReadOnlyList<Event>> GetTransactionEventsAsync(Guid correlationId)
    {
        return await _eventStore.ReadByCorrelationAsync(correlationId);
    }
}
```

---

## Registration and Configuration

### Startup Configuration

```csharp
// appsettings.json
{
  "EventStore": {
    "ConnectionString": "Host=localhost;Database=myapp;Username=user;Password=pass",
    "SnapshotInterval": 10  // Create snapshot every N events
  }
}

// Program.cs
builder.Services.AddEventStore(builder.Configuration);
```

### Extension Method

```csharp
public static class EventStoreExtensions
{
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EventStoreOptions>(
            configuration.GetSection("EventStore"));

        services.AddDbContext<EventStoreDbContext>(options =>
        {
            var connectionString = configuration
                .GetSection("EventStore:ConnectionString").Value;
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IEventStore, PostgresEventStore>();
        services.AddScoped<ISnapshotStore, PostgresSnapshotStore>();

        return services;
    }
}
```

---

## Advanced Features

### Optimistic Concurrency Control

Prevent concurrent writes to the same stream:

```csharp
public async Task AppendWithConcurrencyCheckAsync(string streamId, EventData eventData)
{
    // Get current version
    var events = await _eventStore.ReadStreamAsync(streamId);
    var expectedVersion = events.Count;

    try
    {
        await _eventStore.AppendAsync(
            streamId,
            eventData,
            expectedVersion: expectedVersion
        );
    }
    catch (ConcurrencyException)
    {
        // Another process modified the stream, retry or handle conflict
        throw;
    }
}
```

### Event Projections (Read Models)

Build read models from events:

```csharp
public class UserStatisticsProjection
{
    private readonly IEventStore _eventStore;

    public async Task<UserStatistics> ProjectAsync(string userId)
    {
        var events = await _eventStore.ReadStreamAsync($"user-{userId}");

        var stats = new UserStatistics
        {
            UserId = userId,
            TotalLogins = events.Count(e => e.EventType == "UserLoggedIn"),
            TotalPurchases = events.Count(e => e.EventType == "PurchaseCompleted"),
            LastActivity = events.Max(e => e.Timestamp)
        };

        return stats;
    }
}
```

### Event Versioning

Handle schema evolution:

```csharp
public class EventUpgrader
{
    public object UpgradeEvent(Event @event)
    {
        // Check event version in metadata
        var version = GetEventVersion(@event);

        return (@event.EventType, version) switch
        {
            ("UserCreated", 1) => UpgradeUserCreatedV1ToV2(@event),
            ("UserCreated", 2) => DeserializeV2(@event),
            _ => DeserializeCurrent(@event)
        };
    }

    private object UpgradeUserCreatedV1ToV2(Event @event)
    {
        var v1 = JsonSerializer.Deserialize<UserCreatedV1>(@event.EventData);
        return new UserCreatedV2
        {
            UserId = v1.UserId,
            Email = v1.Email,
            Name = v1.FullName,  // Renamed property
            PhoneNumber = null   // New property with default
        };
    }
}
```

---

## Performance Considerations

### 1. Snapshot Strategy
- Create snapshots every N events (configurable, default: 10)
- Only for streams with frequent reads
- Trade-off: storage vs read performance

### 2. Indexing
- Stream ID (most common query)
- Event type (for projections)
- Timestamp (for time-range queries)
- Correlation ID (for tracing)

### 3. Pagination
- Always use pagination for forward reads
- Default page size: 100 events
- Use `fromPosition` for cursor-based pagination

### 4. Archiving
- Consider archiving old events to separate table
- Keep recent events (e.g., last 90 days) in hot storage
- Move older events to cold storage or S3

---

## Limitations (By Design)

This EventStore is intentionally simple and does NOT include:

❌ **Message bus integration** - Use Conductor for this
❌ **Saga orchestration** - Use Conductor for this
❌ **Distributed transactions** - Use Conductor for this
❌ **Automatic projections** - Build your own read models
❌ **Built-in subscriptions** - Polling-based reads only
❌ **Multi-database support** - PostgreSQL only

---

## Migration from EventStore to Conductor

If your use case evolves and you need workflows/sagas:

```csharp
// Before: Simple event store
await _eventStore.AppendAsync("order-123", new EventData
{
    EventType = "OrderCreated",
    Data = orderData
});

// After: Conductor with saga orchestration
await _publishEndpoint.Publish(new OrderCreatedEvent
{
    OrderId = "123",
    Data = orderData
});

// Conductor saga handles the workflow:
// 1. Reserve inventory
// 2. Charge payment
// 3. Send confirmation
// 4. Compensate on failure
```

**Migration path:** EventStore events can be replayed to bootstrap Conductor sagas.

---

## Testing

### In-Memory Implementation

```csharp
public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<string, List<Event>> _streams = new();
    private long _position = 0;

    public Task AppendAsync(string streamId, EventData eventData, ...)
    {
        if (!_streams.ContainsKey(streamId))
            _streams[streamId] = new List<Event>();

        var stream = _streams[streamId];
        var @event = new Event
        {
            Id = ++_position,
            StreamId = streamId,
            EventType = eventData.EventType,
            EventData = JsonSerializer.Serialize(eventData.Data),
            Version = stream.Count + 1,
            Timestamp = DateTime.UtcNow,
            CorrelationId = eventData.CorrelationId
        };

        stream.Add(@event);
        return Task.CompletedTask;
    }

    // ... other methods
}

// Test usage
services.AddScoped<IEventStore, InMemoryEventStore>();
```

---

## Summary

**AppliedAccountability.EventStore** provides:
- ✅ Simple event persistence (append-only)
- ✅ Event retrieval by stream, type, date, correlation
- ✅ Snapshots for performance
- ✅ PostgreSQL-based storage
- ✅ Optimistic concurrency control
- ✅ Event versioning support

**NOT included** (use Conductor):
- ❌ Message bus / pub-sub
- ❌ Saga orchestration
- ❌ Distributed workflows
- ❌ Job scheduling

**Perfect for:** Audit trails, change tracking, simple CQRS, compliance logging

**When it grows:** Migrate to Conductor for full event-driven architecture
