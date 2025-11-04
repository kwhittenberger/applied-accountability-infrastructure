namespace AppliedAccountability.Data.Entities;

/// <summary>
/// Base class for fully auditable entities with GUID primary key and soft delete support.
/// </summary>
public abstract class FullAuditableEntity : FullAuditableEntity<Guid>
{
    protected FullAuditableEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }
}

/// <summary>
/// Base class for fully auditable entities with specific primary key type.
/// Includes audit fields and soft delete support.
/// </summary>
/// <typeparam name="TKey">Type of the primary key.</typeparam>
public abstract class FullAuditableEntity<TKey> : AuditableEntity<TKey>, ISoftDeletable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the entity was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID or identifier of who deleted the entity.
    /// </summary>
    public string? DeletedBy { get; set; }
}
