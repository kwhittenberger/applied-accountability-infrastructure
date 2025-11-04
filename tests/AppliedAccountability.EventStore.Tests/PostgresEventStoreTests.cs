using AppliedAccountability.EventStore.Abstractions;
using AppliedAccountability.EventStore.Exceptions;
using AppliedAccountability.EventStore.Models;
using AppliedAccountability.EventStore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppliedAccountability.EventStore.Tests;

public class PostgresEventStoreTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly IEventStore _eventStore;
    private readonly Mock<ILogger<PostgresEventStore>> _logger;

    public PostgresEventStoreTests()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase($"EventStoreTest_{Guid.NewGuid()}")
            .Options;

        _context = new EventStoreDbContext(options);
        _logger = new Mock<ILogger<PostgresEventStore>>();
        _eventStore = new PostgresEventStore(_context, _logger.Object);
    }

    [Fact]
    public async Task AppendAsync_SingleEvent_Success()
    {
        // Arrange
        var streamId = "user-123";
        var eventData = new EventData
        {
            EventType = "UserCreated",
            Data = new { UserId = "123", Email = "test@example.com" },
            Metadata = new { UserId = "admin" }
        };

        // Act
        await _eventStore.AppendAsync(streamId, eventData);

        // Assert
        var events = await _eventStore.ReadStreamAsync(streamId);
        Assert.Single(events);
        Assert.Equal("UserCreated", events[0].EventType);
        Assert.Equal(1, events[0].Version);
        Assert.Equal(streamId, events[0].StreamId);
    }

    [Fact]
    public async Task AppendAsync_MultipleEvents_CorrectVersioning()
    {
        // Arrange
        var streamId = "user-123";
        var events = new[]
        {
            new EventData { EventType = "UserCreated", Data = new { UserId = "123" } },
            new EventData { EventType = "UserEmailChanged", Data = new { Email = "new@example.com" } },
            new EventData { EventType = "UserActivated", Data = new { IsActive = true } }
        };

        // Act
        await _eventStore.AppendAsync(streamId, events);

        // Assert
        var storedEvents = await _eventStore.ReadStreamAsync(streamId);
        Assert.Equal(3, storedEvents.Count);
        Assert.Equal(1, storedEvents[0].Version);
        Assert.Equal(2, storedEvents[1].Version);
        Assert.Equal(3, storedEvents[2].Version);
    }

    [Fact]
    public async Task AppendAsync_WithExpectedVersion_Success()
    {
        // Arrange
        var streamId = "user-123";
        await _eventStore.AppendAsync(streamId, new EventData
        {
            EventType = "UserCreated",
            Data = new { UserId = "123" }
        });

        // Act
        await _eventStore.AppendAsync(
            streamId,
            new EventData { EventType = "UserUpdated", Data = new { Name = "John" } },
            expectedVersion: 1);

        // Assert
        var events = await _eventStore.ReadStreamAsync(streamId);
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task AppendAsync_WithWrongExpectedVersion_ThrowsConcurrencyException()
    {
        // Arrange
        var streamId = "user-123";
        await _eventStore.AppendAsync(streamId, new EventData
        {
            EventType = "UserCreated",
            Data = new { UserId = "123" }
        });

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            _eventStore.AppendAsync(
                streamId,
                new EventData { EventType = "UserUpdated", Data = new { Name = "John" } },
                expectedVersion: 5));
    }

    [Fact]
    public async Task ReadStreamAsync_WithVersionRange_ReturnsCorrectEvents()
    {
        // Arrange
        var streamId = "user-123";
        var events = Enumerable.Range(1, 10)
            .Select(i => new EventData
            {
                EventType = $"Event{i}",
                Data = new { Count = i }
            })
            .ToArray();

        await _eventStore.AppendAsync(streamId, events);

        // Act
        var result = await _eventStore.ReadStreamAsync(streamId, fromVersion: 3, toVersion: 7);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(3, result[0].Version);
        Assert.Equal(7, result[4].Version);
    }

    [Fact]
    public async Task ReadStreamAsync_NonExistentStream_ReturnsEmpty()
    {
        // Act
        var events = await _eventStore.ReadStreamAsync("non-existent");

        // Assert
        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadForwardAsync_ReturnsEventsInOrder()
    {
        // Arrange
        await _eventStore.AppendAsync("stream1", new EventData
        {
            EventType = "Event1",
            Data = new { }
        });
        await _eventStore.AppendAsync("stream2", new EventData
        {
            EventType = "Event2",
            Data = new { }
        });
        await _eventStore.AppendAsync("stream3", new EventData
        {
            EventType = "Event3",
            Data = new { }
        });

        // Act
        var events = await _eventStore.ReadForwardAsync(fromPosition: 0, maxCount: 10);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.True(events[0].Id < events[1].Id);
        Assert.True(events[1].Id < events[2].Id);
    }

    [Fact]
    public async Task ReadEventTypeAsync_FiltersCorrectly()
    {
        // Arrange
        await _eventStore.AppendAsync("stream1", new EventData
        {
            EventType = "UserCreated",
            Data = new { UserId = "1" }
        });
        await _eventStore.AppendAsync("stream2", new EventData
        {
            EventType = "UserDeleted",
            Data = new { UserId = "2" }
        });
        await _eventStore.AppendAsync("stream3", new EventData
        {
            EventType = "UserCreated",
            Data = new { UserId = "3" }
        });

        // Act
        var events = await _eventStore.ReadEventTypeAsync("UserCreated");

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("UserCreated", e.EventType));
    }

    [Fact]
    public async Task ReadByCorrelationAsync_ReturnsRelatedEvents()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        await _eventStore.AppendAsync("stream1", new EventData
        {
            EventType = "OrderCreated",
            Data = new { OrderId = "1" },
            CorrelationId = correlationId
        });
        await _eventStore.AppendAsync("stream2", new EventData
        {
            EventType = "PaymentProcessed",
            Data = new { OrderId = "1" },
            CorrelationId = correlationId
        });
        await _eventStore.AppendAsync("stream3", new EventData
        {
            EventType = "OrderShipped",
            Data = new { OrderId = "1" },
            CorrelationId = correlationId
        });

        // Act
        var events = await _eventStore.ReadByCorrelationAsync(correlationId);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal(correlationId, e.CorrelationId));
    }

    [Fact]
    public async Task GetStreamVersionAsync_ReturnsCorrectVersion()
    {
        // Arrange
        var streamId = "user-123";
        await _eventStore.AppendAsync(streamId, new[]
        {
            new EventData { EventType = "Event1", Data = new { } },
            new EventData { EventType = "Event2", Data = new { } },
            new EventData { EventType = "Event3", Data = new { } }
        });

        // Act
        var version = await _eventStore.GetStreamVersionAsync(streamId);

        // Assert
        Assert.Equal(3, version);
    }

    [Fact]
    public async Task GetStreamVersionAsync_NonExistentStream_ReturnsZero()
    {
        // Act
        var version = await _eventStore.GetStreamVersionAsync("non-existent");

        // Assert
        Assert.Equal(0, version);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
