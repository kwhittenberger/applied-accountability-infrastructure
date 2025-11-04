namespace AppliedAccountability.Data.Entities;

/// <summary>
/// Base class for auditable entities with a GUID primary key.
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Base class for auditable entities with a specific primary key type.
/// Includes audit fields for tracking creation and modification.
/// </summary>
/// <typeparam name="TKey">Type of the primary key.</typeparam>
public abstract class AuditableEntity<TKey> : Entity<TKey>, IAuditableEntity
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Timestamp when the entity was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID or identifier of who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID or identifier of who last updated the entity.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
