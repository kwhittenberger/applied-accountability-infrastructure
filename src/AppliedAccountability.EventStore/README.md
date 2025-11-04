# AppliedAccountability.EventStore

Lightweight event store for audit logging, change tracking, and simple event sourcing patterns. PostgreSQL-based with snapshot support.

## Features

- **Event Persistence** - Append-only event storage with JSONB
- **Event Streams** - Group events by aggregate ID
- **Event Queries** - Query by stream, type, date range, correlation ID
- **Snapshots** - Point-in-time state checkpoints for performance
- **Optimistic Concurrency** - Version-based conflict detection
- **PostgreSQL Native** - Leverages JSONB and indexes
- **Lightweight** - No message bus, no sagas, no orchestration

## Installation

```bash
dotnet add package AppliedAccountability.EventStore
```

## Quick Start

### 1. Configure Services

```csharp
using AppliedAccountability.EventStore.Configuration;

// In Program.cs
builder.Services.AddEventStore(builder.Configuration);
```

### 2. appsettings.json

```json
{
  "EventStore": {
    "ConnectionString": "Host=localhost;Database=myapp;Username=user;Password=pass",
    "SnapshotInterval": 10,
    "SnapshotsToKeep": 3,
    "EnableDetailedLogging": false
  }
}
```

### 3. Apply Database Migrations

Create and apply migrations using EF Core tools:

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Add migration
dotnet ef migrations add InitialEventStore --context EventStoreDbContext

# Update database
dotnet ef database update --context EventStoreDbContext
```

## Usage Examples

### Example 1: Audit Trail

```csharp
using AppliedAccountability.EventStore.Abstractions;
using AppliedAccountability.EventStore.Models;

public class UserService
{
    private readonly IEventStore _eventStore;

    public UserService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task CreateUserAsync(string userId, string email, string name)
    {
        // Your business logic here...

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
                    PerformedBy = "admin@example.com",
                    IpAddress = "192.168.1.100"
                }
            }
        );
    }

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
    public string AccountId { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }

    // Apply events to rebuild state
    public void Apply(Event @event)
    {
        switch (@event.EventType)
        {
            case "AccountOpened":
                var opened = JsonSerializer.Deserialize<AccountOpenedEvent>(@event.EventData)!;
                AccountId = opened.AccountId;
                Balance = opened.InitialDeposit;
                IsActive = true;
                break;

            case "MoneyDeposited":
                var deposited = JsonSerializer.Deserialize<MoneyDepositedEvent>(@event.EventData)!;
                Balance += deposited.Amount;
                break;

            case "MoneyWithdrawn":
                var withdrawn = JsonSerializer.Deserialize<MoneyWithdrawnEvent>(@event.EventData)!;
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

        // Load from snapshot if available
        var snapshot = await _snapshotStore.GetLatestSnapshotAsync(streamId);
        int fromVersion = 1;

        if (snapshot != null)
        {
            account = JsonSerializer.Deserialize<Account>(snapshot.StateData)!;
            fromVersion = snapshot.Version + 1;
        }

        // Load events since snapshot
        var events = await _eventStore.ReadStreamAsync(streamId, fromVersion);

        // Replay events
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
        var version = await _eventStore.GetStreamVersionAsync(streamId);
        if (version % 10 == 0)
        {
            var account = await GetAccountAsync(accountId);
            await _snapshotStore.SaveSnapshotAsync(streamId, version, account);
        }
    }
}
```

### Example 3: Optimistic Concurrency Control

```csharp
public async Task UpdateAccountWithConcurrencyCheck(string accountId, decimal amount)
{
    var streamId = $"account-{accountId}";

    // Get current version
    var currentVersion = await _eventStore.GetStreamVersionAsync(streamId);

    try
    {
        // Append with expected version
        await _eventStore.AppendAsync(
            streamId,
            new EventData
            {
                EventType = "MoneyDeposited",
                Data = new { Amount = amount }
            },
            expectedVersion: currentVersion
        );
    }
    catch (ConcurrencyException ex)
    {
        // Handle conflict - retry or notify user
        Console.WriteLine($"Conflict detected. Expected {ex.ExpectedVersion}, actual {ex.ActualVersion}");
        throw;
    }
}
```

### Example 4: Correlation ID Tracking

```csharp
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

    await _eventStore.AppendAsync($"inventory-{orderId}", new EventData
    {
        EventType = "InventoryReserved",
        Data = new { OrderId = orderId, Items = 5 },
        CorrelationId = correlationId
    });

    await _eventStore.AppendAsync($"payment-{orderId}", new EventData
    {
        EventType = "PaymentProcessed",
        Data = new { OrderId = orderId, Amount = 99.99m },
        CorrelationId = correlationId
    });
}

// Get all events for a transaction
public async Task<IReadOnlyList<Event>> GetTransactionEventsAsync(Guid correlationId)
{
    return await _eventStore.ReadByCorrelationAsync(correlationId);
}
```

## API Reference

### IEventStore

```csharp
// Append events
Task AppendAsync(string streamId, EventData eventData, int? expectedVersion = null, ...);
Task AppendAsync(string streamId, IEnumerable<EventData> events, int? expectedVersion = null, ...);

// Read events
Task<IReadOnlyList<Event>> ReadStreamAsync(string streamId, int fromVersion = 1, int? toVersion = null, ...);
Task<IReadOnlyList<Event>> ReadForwardAsync(long fromPosition = 0, int maxCount = 100, ...);
Task<IReadOnlyList<Event>> ReadEventTypeAsync(string eventType, DateTime? fromTimestamp = null, ...);
Task<IReadOnlyList<Event>> ReadByCorrelationAsync(Guid correlationId, ...);

// Get stream version
Task<int> GetStreamVersionAsync(string streamId, ...);
```

### ISnapshotStore

```csharp
// Save snapshot
Task SaveSnapshotAsync(string streamId, int version, object state, ...);

// Get snapshots
Task<Snapshot?> GetLatestSnapshotAsync(string streamId, ...);
Task<Snapshot?> GetSnapshotAsync(string streamId, int version, ...);

// Cleanup
Task DeleteOldSnapshotsAsync(string streamId, int keepCount = 3, ...);
```

## When NOT to Use EventStore

❌ Use **[Conductor](https://github.com/yourusername/conductor)** instead if you need:
- Distributed workflows or sagas
- Message bus integration (RabbitMQ, etc.)
- Job scheduling or background processing
- Compensation transactions
- Long-running orchestration

EventStore is for simple event persistence. For complex orchestration, use Conductor.

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
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    correlation_id UUID NOT NULL,
    CONSTRAINT unique_stream_version UNIQUE (stream_id, version)
);

CREATE INDEX idx_events_stream_id ON events (stream_id);
CREATE INDEX idx_events_event_type ON events (event_type);
CREATE INDEX idx_events_timestamp ON events (timestamp);
CREATE INDEX idx_events_correlation_id ON events (correlation_id);
```

### snapshots Table
```sql
CREATE TABLE snapshots (
    id BIGSERIAL PRIMARY KEY,
    stream_id VARCHAR(255) NOT NULL,
    version INTEGER NOT NULL,
    state_data JSONB NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT unique_stream_snapshot UNIQUE (stream_id, version)
);

CREATE INDEX idx_snapshots_stream_id ON snapshots (stream_id);
```

## Requirements

- .NET 10.0 or later
- PostgreSQL 13+ with JSONB support
- Entity Framework Core 9.0+

## License

MIT License - Copyright © Applied Accountability Services LLC 2025

## Documentation

For more detailed information, see:
- [EVENTSTORE_DESIGN.md](../../EVENTSTORE_DESIGN.md) - Complete design document
- [CONDUCTOR_VS_INFRASTRUCTURE.md](../../CONDUCTOR_VS_INFRASTRUCTURE.md) - When to use what

## Contributing

This package is maintained by Applied Accountability Services LLC. For bug reports or feature requests, please open an issue on GitHub.
