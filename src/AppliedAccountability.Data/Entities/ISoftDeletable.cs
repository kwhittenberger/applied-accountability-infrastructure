namespace AppliedAccountability.Data.Entities;

/// <summary>
/// Interface for entities that support soft delete (logical delete).
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the entity was deleted (UTC).
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID or identifier of who deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}
