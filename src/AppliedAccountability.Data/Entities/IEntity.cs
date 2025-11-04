namespace AppliedAccountability.Data.Entities;

/// <summary>
/// Base interface for all entities with a primary key.
/// </summary>
/// <typeparam name="TKey">Type of the primary key.</typeparam>
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    TKey Id { get; set; }
}
