namespace AppliedAccountability.Data.Entities;

/// <summary>
/// Interface for entities that track creation and modification audit information.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Timestamp when the entity was created (UTC).
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID or identifier of who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated (UTC).
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID or identifier of who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; set; }
}
