using AppliedAccountability.EventStore.Models;
using Microsoft.EntityFrameworkCore;

namespace AppliedAccountability.EventStore.Persistence;

/// <summary>
/// Database context for event store.
/// </summary>
public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Events table.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Snapshots table.
    /// </summary>
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Events table configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StreamId)
                .HasColumnName("stream_id")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.EventData)
                .HasColumnName("event_data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

            entity.Property(e => e.CorrelationId)
                .HasColumnName("correlation_id")
                .IsRequired();

            // Unique constraint on stream_id + version
            entity.HasIndex(e => new { e.StreamId, e.Version })
                .IsUnique()
                .HasDatabaseName("idx_events_stream_version");

            // Indexes for efficient queries
            entity.HasIndex(e => e.StreamId)
                .HasDatabaseName("idx_events_stream_id");

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("idx_events_event_type");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("idx_events_timestamp");

            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("idx_events_correlation_id");
        });

        // Snapshots table configuration
        modelBuilder.Entity<Snapshot>(entity =>
        {
            entity.ToTable("snapshots");

            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(s => s.StreamId)
                .HasColumnName("stream_id")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(s => s.Version)
                .HasColumnName("version")
                .IsRequired();

            entity.Property(s => s.StateData)
                .HasColumnName("state_data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(s => s.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

            // Unique constraint on stream_id + version
            entity.HasIndex(s => new { s.StreamId, s.Version })
                .IsUnique()
                .HasDatabaseName("idx_snapshots_stream_version");

            // Index for stream queries
            entity.HasIndex(s => s.StreamId)
                .HasDatabaseName("idx_snapshots_stream_id");
        });
    }
}
