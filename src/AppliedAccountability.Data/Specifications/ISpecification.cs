using System.Linq.Expressions;

namespace AppliedAccountability.Data.Specifications;

/// <summary>
/// Specification pattern interface for building complex queries.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the filter criteria expression.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the list of include expressions (for eager loading).
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of include strings (for eager loading by property name).
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by descending expression.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the number of records to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the number of records to take.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets whether tracking is enabled.
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// Gets whether to ignore query filters (like soft delete filter).
    /// </summary>
    bool IgnoreQueryFilters { get; }
}
