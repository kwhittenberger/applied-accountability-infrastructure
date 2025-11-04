using AppliedAccountability.EventStore.Abstractions;
using AppliedAccountability.EventStore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppliedAccountability.EventStore.Tests;

public class PostgresSnapshotStoreTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly ISnapshotStore _snapshotStore;
    private readonly Mock<ILogger<PostgresSnapshotStore>> _logger;

    public PostgresSnapshotStoreTests()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase($"SnapshotStoreTest_{Guid.NewGuid()}")
            .Options;

        _context = new EventStoreDbContext(options);
        _logger = new Mock<ILogger<PostgresSnapshotStore>>();
        _snapshotStore = new PostgresSnapshotStore(_context, _logger.Object);
    }

    [Fact]
    public async Task SaveSnapshotAsync_Success()
    {
        // Arrange
        var streamId = "account-123";
        var version = 10;
        var state = new { AccountId = "123", Balance = 1000m, IsActive = true };

        // Act
        await _snapshotStore.SaveSnapshotAsync(streamId, version, state);

        // Assert
        var snapshot = await _snapshotStore.GetLatestSnapshotAsync(streamId);
        Assert.NotNull(snapshot);
        Assert.Equal(streamId, snapshot.StreamId);
        Assert.Equal(version, snapshot.Version);
        Assert.Contains("\"balance\":1000", snapshot.StateData);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_ReturnsLatest()
    {
        // Arrange
        var streamId = "account-123";
        await _snapshotStore.SaveSnapshotAsync(streamId, 5, new { Balance = 500m });
        await _snapshotStore.SaveSnapshotAsync(streamId, 10, new { Balance = 1000m });
        await _snapshotStore.SaveSnapshotAsync(streamId, 15, new { Balance = 1500m });

        // Act
        var snapshot = await _snapshotStore.GetLatestSnapshotAsync(streamId);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(15, snapshot.Version);
        Assert.Contains("\"balance\":1500", snapshot.StateData);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_NonExistent_ReturnsNull()
    {
        // Act
        var snapshot = await _snapshotStore.GetLatestSnapshotAsync("non-existent");

        // Assert
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetSnapshotAsync_SpecificVersion_ReturnsCorrect()
    {
        // Arrange
        var streamId = "account-123";
        await _snapshotStore.SaveSnapshotAsync(streamId, 5, new { Balance = 500m });
        await _snapshotStore.SaveSnapshotAsync(streamId, 10, new { Balance = 1000m });
        await _snapshotStore.SaveSnapshotAsync(streamId, 15, new { Balance = 1500m });

        // Act
        var snapshot = await _snapshotStore.GetSnapshotAsync(streamId, 10);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(10, snapshot.Version);
        Assert.Contains("\"balance\":1000", snapshot.StateData);
    }

    [Fact]
    public async Task GetSnapshotAsync_NonExistentVersion_ReturnsNull()
    {
        // Arrange
        var streamId = "account-123";
        await _snapshotStore.SaveSnapshotAsync(streamId, 10, new { Balance = 1000m });

        // Act
        var snapshot = await _snapshotStore.GetSnapshotAsync(streamId, 5);

        // Assert
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_KeepsLatestN()
    {
        // Arrange
        var streamId = "account-123";
        for (int i = 1; i <= 10; i++)
        {
            await _snapshotStore.SaveSnapshotAsync(streamId, i, new { Balance = i * 100m });
        }

        // Act
        await _snapshotStore.DeleteOldSnapshotsAsync(streamId, keepCount: 3);

        // Assert
        var allSnapshots = await _context.Snapshots
            .Where(s => s.StreamId == streamId)
            .OrderBy(s => s.Version)
            .ToListAsync();

        Assert.Equal(3, allSnapshots.Count);
        Assert.Equal(8, allSnapshots[0].Version);
        Assert.Equal(9, allSnapshots[1].Version);
        Assert.Equal(10, allSnapshots[2].Version);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithFewerThanKeepCount_DoesNotDelete()
    {
        // Arrange
        var streamId = "account-123";
        await _snapshotStore.SaveSnapshotAsync(streamId, 1, new { Balance = 100m });
        await _snapshotStore.SaveSnapshotAsync(streamId, 2, new { Balance = 200m });

        // Act
        await _snapshotStore.DeleteOldSnapshotsAsync(streamId, keepCount: 5);

        // Assert
        var allSnapshots = await _context.Snapshots
            .Where(s => s.StreamId == streamId)
            .ToListAsync();

        Assert.Equal(2, allSnapshots.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
