namespace AppliedAccountability.Data.Entities;

/// <summary>
/// Base class for entities with a GUID primary key.
/// </summary>
public abstract class Entity : Entity<Guid>
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Base class for entities with a specific primary key type.
/// </summary>
/// <typeparam name="TKey">Type of the primary key.</typeparam>
public abstract class Entity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public virtual TKey Id { get; set; } = default!;

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id.Equals(default(TKey)) || other.Id.Equals(default(TKey)))
            return false;

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Entity<TKey>? a, Entity<TKey>? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Entity<TKey>? a, Entity<TKey>? b)
    {
        return !(a == b);
    }
}
