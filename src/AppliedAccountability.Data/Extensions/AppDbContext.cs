using AppliedAccountability.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppliedAccountability.Data.Extensions;

/// <summary>
/// Base DbContext with automatic audit field handling and soft delete support.
/// </summary>
public abstract class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    protected AppDbContext(DbContextOptions options) : base(options)
    {
    }

    protected AppDbContext(DbContextOptions options, ICurrentUserService? currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        OnBeforeSaveChanges();
        return base.SaveChanges();
    }

    private void OnBeforeSaveChanges()
    {
        var now = DateTime.UtcNow;
        var currentUser = _currentUserService?.GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries())
        {
            // Handle auditable entities
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditableEntity.CreatedAt = now;
                        auditableEntity.CreatedBy = currentUser;
                        break;

                    case EntityState.Modified:
                        auditableEntity.UpdatedAt = now;
                        auditableEntity.UpdatedBy = currentUser;
                        break;
                }
            }

            // Handle soft deletable entities
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = currentUser;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// Interface for providing the current user context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's identifier.
    /// </summary>
    string? GetCurrentUserId();
}
